using System;
using System.Linq;
using System.Threading.Tasks;
using Payments_core.Models.BBPS;
using Payments_core.Services.BBPSService.Repository;
using Payments_core.Services.WalletService;

namespace Payments_core.Services.BBPSService
{
    public class BbpsReconService
    {
        private readonly IBbpsRepository _repo;
        private readonly IBbpsService _bbps;
        private readonly IWalletService _wallet;

        public BbpsReconService(
            IBbpsRepository repo,
            IBbpsService bbps,
            IWalletService wallet)
        {
            _repo = repo;
            _bbps = bbps;
            _wallet = wallet;
        }

        /// <summary>
        /// NPCI-safe BBPS reconciliation job
        /// </summary>
        public async Task RunRecon()
        {
            var pendingTxns = (await _repo.GetPendingTransactions())?.ToList();

            if (pendingTxns == null || !pendingTxns.Any())
            {
                Console.WriteLine("ℹ️ BBPS Recon: No pending transactions");
                return;
            }

            foreach (var txn in pendingTxns)
            {
                try
                {
                    var statusResponse = await _bbps.CheckStatus(
                        txn.TxnRefId,
                        txn.BillRequestId
                    );

                    var finalStatus =
                        statusResponse.Status?.ToUpperInvariant();

                    if (finalStatus == "SUCCESS")
                    {
                        await _wallet.FinalizeDebit(
                            txn.UserId,
                            txn.Amount,
                            txn.TxnRefId,
                            "BBPS Recon Success"
                        );

                        await _repo.UpdateReconAttempt(
                            txn.TxnRefId,
                            "SUCCESS",
                            statusResponse.RawXml
                        );
                    }
                    else if (finalStatus == "FAILED" || finalStatus == "REVERSED")
                    {
                        await _wallet.ReverseHold(
                            txn.UserId,
                            txn.Amount,
                            txn.TxnRefId,
                            "BBPS Recon Failed"
                        );

                        await _repo.UpdateReconAttempt(
                            txn.TxnRefId,
                            finalStatus,
                            statusResponse.RawXml
                        );
                    }
                    else
                    {
                        // Still pending → just update retry_count + timestamp
                        await _repo.UpdateReconAttempt(
                            txn.TxnRefId,
                            "PENDING",
                            statusResponse.RawXml
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"🔥 Recon ERROR | TxnRefId={txn.TxnRefId} | {ex.Message}"
                    );
                }
            }
        }
    }
}