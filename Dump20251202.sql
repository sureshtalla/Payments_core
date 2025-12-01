CREATE DATABASE  IF NOT EXISTS `payments_core` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `payments_core`;
-- MySQL dump 10.13  Distrib 8.0.44, for Win64 (x86_64)
--
-- Host: localhost    Database: payments_core
-- ------------------------------------------------------
-- Server version	8.0.44

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `audit_logs`
--

DROP TABLE IF EXISTS `audit_logs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `audit_logs` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `user_id` bigint unsigned DEFAULT NULL,
  `action` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `entity_type` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `entity_id` bigint unsigned DEFAULT NULL,
  `details` json DEFAULT NULL,
  `ip_address` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  CONSTRAINT `audit_logs_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `audit_logs`
--

LOCK TABLES `audit_logs` WRITE;
/*!40000 ALTER TABLE `audit_logs` DISABLE KEYS */;
INSERT INTO `audit_logs` VALUES (1,1,'APPROVE_KYC','MERCHANT',1,'{\"message\": \"Merchant approved\"}',NULL,'2025-11-29 06:47:37');
/*!40000 ALTER TABLE `audit_logs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `bbps_billers`
--

DROP TABLE IF EXISTS `bbps_billers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bbps_billers` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `provider_id` int unsigned DEFAULT NULL,
  `biller_code` varchar(128) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `bank_name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `category` varchar(64) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `status` enum('ACTIVE','INACTIVE') COLLATE utf8mb4_unicode_ci DEFAULT 'ACTIVE',
  `metadata_json` json DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `provider_id` (`provider_id`),
  CONSTRAINT `bbps_billers_ibfk_1` FOREIGN KEY (`provider_id`) REFERENCES `providers` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bbps_billers`
--

LOCK TABLES `bbps_billers` WRITE;
/*!40000 ALTER TABLE `bbps_billers` DISABLE KEYS */;
INSERT INTO `bbps_billers` VALUES (1,3,'VISA123','HDFC Bank Credit Card','CREDIT_CARD_BILL','ACTIVE',NULL);
/*!40000 ALTER TABLE `bbps_billers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `bbps_transactions`
--

DROP TABLE IF EXISTS `bbps_transactions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bbps_transactions` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `provider_id` int unsigned DEFAULT NULL,
  `bbps_ref` varchar(128) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `pg_txn_id` bigint unsigned DEFAULT NULL,
  `created_by_user` bigint unsigned DEFAULT NULL,
  `biller_id` bigint unsigned DEFAULT NULL,
  `card_bank_name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `customer_ref` varchar(128) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `amount` decimal(18,2) DEFAULT NULL,
  `status` enum('PENDING','SUCCESS','FAILED','REVERSED') COLLATE utf8mb4_unicode_ci DEFAULT 'PENDING',
  `receipt_data` json DEFAULT NULL,
  `initiated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `completed_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `provider_id` (`provider_id`),
  KEY `pg_txn_id` (`pg_txn_id`),
  KEY `created_by_user` (`created_by_user`),
  KEY `biller_id` (`biller_id`),
  KEY `idx_bbps_status` (`status`),
  CONSTRAINT `bbps_transactions_ibfk_1` FOREIGN KEY (`provider_id`) REFERENCES `providers` (`id`),
  CONSTRAINT `bbps_transactions_ibfk_2` FOREIGN KEY (`pg_txn_id`) REFERENCES `pg_transactions` (`id`),
  CONSTRAINT `bbps_transactions_ibfk_3` FOREIGN KEY (`created_by_user`) REFERENCES `users` (`id`),
  CONSTRAINT `bbps_transactions_ibfk_4` FOREIGN KEY (`biller_id`) REFERENCES `bbps_billers` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bbps_transactions`
--

LOCK TABLES `bbps_transactions` WRITE;
/*!40000 ALTER TABLE `bbps_transactions` DISABLE KEYS */;
/*!40000 ALTER TABLE `bbps_transactions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `commission_schemes`
--

DROP TABLE IF EXISTS `commission_schemes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `commission_schemes` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `name` varchar(150) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `category` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `product_type` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `admin_percent` decimal(5,2) DEFAULT '0.00',
  `sd_percent` decimal(5,2) DEFAULT '0.00',
  `distributor_percent` decimal(5,2) DEFAULT '0.00',
  `retailer_percent` decimal(5,2) DEFAULT '0.00',
  `effective_from` date DEFAULT NULL,
  `effective_to` date DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `commission_schemes`
--

LOCK TABLES `commission_schemes` WRITE;
/*!40000 ALTER TABLE `commission_schemes` DISABLE KEYS */;
INSERT INTO `commission_schemes` VALUES (1,'Default PG Commission','PG','TRAVEL',0.10,0.20,0.10,0.60,NULL,NULL,'2025-11-29 06:43:52');
/*!40000 ALTER TABLE `commission_schemes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `kyc_documents`
--

DROP TABLE IF EXISTS `kyc_documents`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `kyc_documents` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `user_id` bigint unsigned NOT NULL,
  `doc_type` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `file_path` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `status` enum('PENDING','APPROVED','REJECTED') COLLATE utf8mb4_unicode_ci DEFAULT 'PENDING',
  `remarks` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `uploaded_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `reviewed_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  CONSTRAINT `kyc_documents_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `kyc_documents`
--

LOCK TABLES `kyc_documents` WRITE;
/*!40000 ALTER TABLE `kyc_documents` DISABLE KEYS */;
INSERT INTO `kyc_documents` VALUES (1,40,'PAN','/docs/merchant1/pan.jpg','APPROVED',NULL,'2025-11-29 06:18:29',NULL),(2,40,'GST','/docs/merchant1/gst.jpg','APPROVED',NULL,'2025-11-29 06:18:29',NULL),(3,40,'BANK_PROOF','/docs/merchant1/bank.jpg','APPROVED',NULL,'2025-11-29 06:18:29',NULL);
/*!40000 ALTER TABLE `kyc_documents` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `kyc_profiles`
--

DROP TABLE IF EXISTS `kyc_profiles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `kyc_profiles` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `user_id` bigint unsigned NOT NULL,
  `pan_number` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `aadhaar_last4` char(4) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `gstin` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `business_type` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `address_line1` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `address_line2` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `city` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `state` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `pincode` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `bank_account_no` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `bank_ifsc` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `risk_category` enum('LOW','MEDIUM','HIGH') COLLATE utf8mb4_unicode_ci DEFAULT 'LOW',
  `kyc_status` enum('PENDING','VERIFIED','REJECTED') COLLATE utf8mb4_unicode_ci DEFAULT 'PENDING',
  `kyc_notes` text COLLATE utf8mb4_unicode_ci,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  CONSTRAINT `kyc_profiles_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `kyc_profiles`
--

LOCK TABLES `kyc_profiles` WRITE;
/*!40000 ALTER TABLE `kyc_profiles` DISABLE KEYS */;
INSERT INTO `kyc_profiles` VALUES (1,40,'ABCDE1234F',NULL,'37ABCDE1234F1Z5',NULL,NULL,NULL,NULL,NULL,NULL,'1234567890','HDFC0001234','LOW','VERIFIED',NULL,'2025-11-29 06:17:50','2025-11-29 06:17:50');
/*!40000 ALTER TABLE `kyc_profiles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `login_otps`
--

DROP TABLE IF EXISTS `login_otps`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `login_otps` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `user_id` bigint NOT NULL,
  `mobile` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `otp` varchar(6) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `created_at` datetime DEFAULT NULL,
  `is_used` tinyint DEFAULT '0',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `login_otps`
--

LOCK TABLES `login_otps` WRITE;
/*!40000 ALTER TABLE `login_otps` DISABLE KEYS */;
INSERT INTO `login_otps` VALUES (1,47,'8008521222','323323','2025-12-02 02:32:04',1),(2,47,'8008521222','211113','2025-12-02 02:39:33',1);
/*!40000 ALTER TABLE `login_otps` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `mdr_pricing`
--

DROP TABLE IF EXISTS `mdr_pricing`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `mdr_pricing` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `category` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `card_type` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `provider_id` int unsigned DEFAULT NULL,
  `slab_min_amount` decimal(18,2) DEFAULT NULL,
  `slab_max_amount` decimal(18,2) DEFAULT NULL,
  `mdr_percent` decimal(6,4) DEFAULT NULL,
  `fixed_fee` decimal(18,2) DEFAULT NULL,
  `effective_from` date DEFAULT NULL,
  `effective_to` date DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `provider_id` (`provider_id`),
  CONSTRAINT `mdr_pricing_ibfk_1` FOREIGN KEY (`provider_id`) REFERENCES `providers` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `mdr_pricing`
--

LOCK TABLES `mdr_pricing` WRITE;
/*!40000 ALTER TABLE `mdr_pricing` DISABLE KEYS */;
INSERT INTO `mdr_pricing` VALUES (1,'TRAVEL','CREDIT',1,0.00,50000.00,0.9500,NULL,NULL,NULL),(2,'EDUCATION','CREDIT',1,0.00,50000.00,0.7500,NULL,NULL,NULL),(3,'ECOMMERCE','CREDIT',1,0.00,50000.00,1.2000,NULL,NULL,NULL);
/*!40000 ALTER TABLE `mdr_pricing` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `merchants`
--

DROP TABLE IF EXISTS `merchants`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `merchants` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `user_id` bigint unsigned NOT NULL,
  `legal_name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `trade_name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `business_type` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `category` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `website_url` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `kyc_status` enum('PENDING','VERIFIED','REJECTED') COLLATE utf8mb4_unicode_ci DEFAULT 'PENDING',
  `risk_category` enum('LOW','MEDIUM','HIGH') COLLATE utf8mb4_unicode_ci DEFAULT 'LOW',
  `settlement_profile` varchar(32) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `enabled_products` text COLLATE utf8mb4_unicode_ci,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  CONSTRAINT `merchants_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `merchants`
--

LOCK TABLES `merchants` WRITE;
/*!40000 ALTER TABLE `merchants` DISABLE KEYS */;
INSERT INTO `merchants` VALUES (1,40,'Classic Travels Pvt Ltd','Classic Travels','PVT_LTD','TRAVEL','https://classictravels.in','VERIFIED','LOW','T_PLUS_1','PG_TRAVEL,BBPS_CC_BILL,PAYOUT,WALLET','2025-11-29 06:15:48','2025-11-29 06:15:48');
/*!40000 ALTER TABLE `merchants` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `payouts`
--

DROP TABLE IF EXISTS `payouts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `payouts` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `provider_id` int unsigned DEFAULT NULL,
  `wallet_id` bigint unsigned DEFAULT NULL,
  `user_id` bigint unsigned DEFAULT NULL,
  `amount` decimal(18,2) DEFAULT NULL,
  `fee_amount` decimal(18,2) DEFAULT NULL,
  `mode` enum('IMPS','NEFT','UPI','BANK_TRANSFER') COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `beneficiary_name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `beneficiary_account_no` varchar(64) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `beneficiary_ifsc` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `beneficiary_vpa` varchar(128) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `provider_ref` varchar(128) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `status` enum('INITIATED','PENDING','SUCCESS','FAILED','REVERSED') COLLATE utf8mb4_unicode_ci DEFAULT 'INITIATED',
  `reason` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `initiated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `completed_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `provider_id` (`provider_id`),
  KEY `wallet_id` (`wallet_id`),
  KEY `user_id` (`user_id`),
  CONSTRAINT `payouts_ibfk_1` FOREIGN KEY (`provider_id`) REFERENCES `providers` (`id`),
  CONSTRAINT `payouts_ibfk_2` FOREIGN KEY (`wallet_id`) REFERENCES `wallets` (`id`),
  CONSTRAINT `payouts_ibfk_3` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=3002 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `payouts`
--

LOCK TABLES `payouts` WRITE;
/*!40000 ALTER TABLE `payouts` DISABLE KEYS */;
INSERT INTO `payouts` VALUES (3001,2,5,40,4800.00,NULL,'IMPS','Classic Travels','22233344455','HDFC0002222',NULL,NULL,'SUCCESS',NULL,'2025-11-29 06:46:08',NULL);
/*!40000 ALTER TABLE `payouts` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `pg_refunds`
--

DROP TABLE IF EXISTS `pg_refunds`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `pg_refunds` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `pg_txn_id` bigint unsigned NOT NULL,
  `provider_ref` varchar(128) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `amount` decimal(18,2) DEFAULT NULL,
  `status` enum('INITIATED','SUCCESS','FAILED') COLLATE utf8mb4_unicode_ci DEFAULT 'INITIATED',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `completed_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `pg_txn_id` (`pg_txn_id`),
  CONSTRAINT `pg_refunds_ibfk_1` FOREIGN KEY (`pg_txn_id`) REFERENCES `pg_transactions` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `pg_refunds`
--

LOCK TABLES `pg_refunds` WRITE;
/*!40000 ALTER TABLE `pg_refunds` DISABLE KEYS */;
/*!40000 ALTER TABLE `pg_refunds` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `pg_routes`
--

DROP TABLE IF EXISTS `pg_routes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `pg_routes` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `category` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `provider_id` int unsigned DEFAULT NULL,
  `priority` int DEFAULT '1',
  `status` enum('ACTIVE','INACTIVE') COLLATE utf8mb4_unicode_ci DEFAULT 'ACTIVE',
  `notes` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `provider_id` (`provider_id`),
  CONSTRAINT `pg_routes_ibfk_1` FOREIGN KEY (`provider_id`) REFERENCES `providers` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `pg_routes`
--

LOCK TABLES `pg_routes` WRITE;
/*!40000 ALTER TABLE `pg_routes` DISABLE KEYS */;
INSERT INTO `pg_routes` VALUES (1,'EDUCATION',1,1,'ACTIVE',NULL),(2,'TRAVEL',1,1,'ACTIVE',NULL),(3,'ECOMMERCE',1,1,'ACTIVE',NULL);
/*!40000 ALTER TABLE `pg_routes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `pg_transactions`
--

DROP TABLE IF EXISTS `pg_transactions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `pg_transactions` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `external_ref` varchar(128) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `provider_id` int unsigned DEFAULT NULL,
  `category` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `created_by_user` bigint unsigned DEFAULT NULL,
  `merchant_id` bigint unsigned DEFAULT NULL,
  `amount` decimal(18,2) NOT NULL,
  `currency` char(3) COLLATE utf8mb4_unicode_ci DEFAULT 'INR',
  `status` enum('INITIATED','PENDING','SUCCESS','FAILED','REFUNDED','CHARGEBACK') COLLATE utf8mb4_unicode_ci DEFAULT 'INITIATED',
  `mdr_percent` decimal(6,4) DEFAULT NULL,
  `mdr_amount` decimal(18,2) DEFAULT NULL,
  `provider_fee` decimal(18,2) DEFAULT NULL,
  `finx_margin` decimal(18,2) DEFAULT NULL,
  `customer_card_last4` varchar(8) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `customer_card_network` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `callback_url` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `provider_payload` json DEFAULT NULL,
  `request_id` varchar(128) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `initiated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `completed_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `request_id` (`request_id`),
  KEY `provider_id` (`provider_id`),
  KEY `created_by_user` (`created_by_user`),
  KEY `merchant_id` (`merchant_id`),
  KEY `idx_pgtxn_status` (`status`),
  KEY `idx_pgtxn_request_id` (`request_id`),
  CONSTRAINT `pg_transactions_ibfk_1` FOREIGN KEY (`provider_id`) REFERENCES `providers` (`id`),
  CONSTRAINT `pg_transactions_ibfk_2` FOREIGN KEY (`created_by_user`) REFERENCES `users` (`id`),
  CONSTRAINT `pg_transactions_ibfk_3` FOREIGN KEY (`merchant_id`) REFERENCES `merchants` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=1002 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `pg_transactions`
--

LOCK TABLES `pg_transactions` WRITE;
/*!40000 ALTER TABLE `pg_transactions` DISABLE KEYS */;
INSERT INTO `pg_transactions` VALUES (1001,'CFX-TRAVEL-8989',1,'TRAVEL',30,1,5000.00,'INR','SUCCESS',0.9500,47.50,20.00,27.50,NULL,NULL,NULL,NULL,'REQ-1001','2025-11-29 06:39:51',NULL);
/*!40000 ALTER TABLE `pg_transactions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `provider_api_keys`
--

DROP TABLE IF EXISTS `provider_api_keys`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `provider_api_keys` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `provider_id` int unsigned DEFAULT NULL,
  `environment` enum('SANDBOX','PRODUCTION') COLLATE utf8mb4_unicode_ci DEFAULT 'SANDBOX',
  `api_key` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `api_secret` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `extra_config` json DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `provider_id` (`provider_id`),
  CONSTRAINT `provider_api_keys_ibfk_1` FOREIGN KEY (`provider_id`) REFERENCES `providers` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `provider_api_keys`
--

LOCK TABLES `provider_api_keys` WRITE;
/*!40000 ALTER TABLE `provider_api_keys` DISABLE KEYS */;
/*!40000 ALTER TABLE `provider_api_keys` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `provider_callbacks`
--

DROP TABLE IF EXISTS `provider_callbacks`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `provider_callbacks` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `txn_id` bigint unsigned DEFAULT NULL,
  `provider_txn_id` varchar(128) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `callback_payload` json DEFAULT NULL,
  `status` enum('RECEIVED','PROCESSED') COLLATE utf8mb4_unicode_ci DEFAULT 'RECEIVED',
  `received_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `processed_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `txn_id` (`txn_id`),
  CONSTRAINT `provider_callbacks_ibfk_1` FOREIGN KEY (`txn_id`) REFERENCES `pg_transactions` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `provider_callbacks`
--

LOCK TABLES `provider_callbacks` WRITE;
/*!40000 ALTER TABLE `provider_callbacks` DISABLE KEYS */;
/*!40000 ALTER TABLE `provider_callbacks` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `providers`
--

DROP TABLE IF EXISTS `providers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `providers` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `code` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `type` enum('PG','BBPS','PAYOUT','VAM') COLLATE utf8mb4_unicode_ci DEFAULT 'PG',
  `status` enum('ACTIVE','INACTIVE') COLLATE utf8mb4_unicode_ci DEFAULT 'ACTIVE',
  `config_json` json DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `providers`
--

LOCK TABLES `providers` WRITE;
/*!40000 ALTER TABLE `providers` DISABLE KEYS */;
INSERT INTO `providers` VALUES (1,'CASHFREE_PG','Cashfree Payment Gateway','PG','ACTIVE',NULL,'2025-11-29 06:20:24'),(2,'CASHFREE_PAYOUT','Cashfree Payouts','PAYOUT','ACTIVE',NULL,'2025-11-29 06:20:24'),(3,'PAYSPRINT_BBPS','Paysprint BBPS','BBPS','ACTIVE',NULL,'2025-11-29 06:20:24');
/*!40000 ALTER TABLE `providers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `roles`
--

DROP TABLE IF EXISTS `roles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `roles` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `roles`
--

LOCK TABLES `roles` WRITE;
/*!40000 ALTER TABLE `roles` DISABLE KEYS */;
INSERT INTO `roles` VALUES (1,'ADMIN','Platform Super Admin','2025-11-29 06:12:00'),(2,'SUPER_DISTRIBUTOR','Top level partner','2025-11-29 06:12:00'),(3,'DISTRIBUTOR','Distributor under SD','2025-11-29 06:12:00'),(4,'RETAILER','Retail agent','2025-11-29 06:12:00'),(5,'MERCHANT','Merchant using PG','2025-11-29 06:12:00'),(6,'SUPPORT','Internal Support Team','2025-11-29 06:12:00');
/*!40000 ALTER TABLE `roles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `settlement_batches`
--

DROP TABLE IF EXISTS `settlement_batches`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `settlement_batches` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `batch_ref` varchar(128) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `user_id` bigint unsigned DEFAULT NULL,
  `total_amount` decimal(18,2) DEFAULT NULL,
  `total_fee` decimal(18,2) DEFAULT NULL,
  `settlement_mode` varchar(32) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `status` enum('CREATED','PROCESSING','COMPLETED','FAILED') COLLATE utf8mb4_unicode_ci DEFAULT 'CREATED',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `completed_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  CONSTRAINT `settlement_batches_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `settlement_batches`
--

LOCK TABLES `settlement_batches` WRITE;
/*!40000 ALTER TABLE `settlement_batches` DISABLE KEYS */;
/*!40000 ALTER TABLE `settlement_batches` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `settlement_items`
--

DROP TABLE IF EXISTS `settlement_items`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `settlement_items` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `batch_id` bigint unsigned DEFAULT NULL,
  `pg_txn_id` bigint unsigned DEFAULT NULL,
  `bbps_txn_id` bigint unsigned DEFAULT NULL,
  `amount` decimal(18,2) DEFAULT NULL,
  `fee_amount` decimal(18,2) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `batch_id` (`batch_id`),
  KEY `pg_txn_id` (`pg_txn_id`),
  KEY `bbps_txn_id` (`bbps_txn_id`),
  CONSTRAINT `settlement_items_ibfk_1` FOREIGN KEY (`batch_id`) REFERENCES `settlement_batches` (`id`),
  CONSTRAINT `settlement_items_ibfk_2` FOREIGN KEY (`pg_txn_id`) REFERENCES `pg_transactions` (`id`),
  CONSTRAINT `settlement_items_ibfk_3` FOREIGN KEY (`bbps_txn_id`) REFERENCES `bbps_transactions` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `settlement_items`
--

LOCK TABLES `settlement_items` WRITE;
/*!40000 ALTER TABLE `settlement_items` DISABLE KEYS */;
/*!40000 ALTER TABLE `settlement_items` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `support_messages`
--

DROP TABLE IF EXISTS `support_messages`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `support_messages` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `ticket_id` bigint unsigned DEFAULT NULL,
  `sender_user_id` bigint unsigned DEFAULT NULL,
  `message` text COLLATE utf8mb4_unicode_ci,
  `is_internal` tinyint(1) DEFAULT '0',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `ticket_id` (`ticket_id`),
  KEY `sender_user_id` (`sender_user_id`),
  CONSTRAINT `support_messages_ibfk_1` FOREIGN KEY (`ticket_id`) REFERENCES `support_tickets` (`id`),
  CONSTRAINT `support_messages_ibfk_2` FOREIGN KEY (`sender_user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `support_messages`
--

LOCK TABLES `support_messages` WRITE;
/*!40000 ALTER TABLE `support_messages` DISABLE KEYS */;
/*!40000 ALTER TABLE `support_messages` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `support_tickets`
--

DROP TABLE IF EXISTS `support_tickets`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `support_tickets` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `created_by_user` bigint unsigned DEFAULT NULL,
  `assigned_to_user` bigint unsigned DEFAULT NULL,
  `subject` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  `category` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `priority` enum('LOW','MEDIUM','HIGH','CRITICAL') COLLATE utf8mb4_unicode_ci DEFAULT 'LOW',
  `status` enum('OPEN','IN_PROGRESS','RESOLVED','CLOSED') COLLATE utf8mb4_unicode_ci DEFAULT 'OPEN',
  `related_pg_txn_id` bigint unsigned DEFAULT NULL,
  `related_bbps_txn_id` bigint unsigned DEFAULT NULL,
  `related_payout_id` bigint unsigned DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `created_by_user` (`created_by_user`),
  CONSTRAINT `support_tickets_ibfk_1` FOREIGN KEY (`created_by_user`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=4002 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `support_tickets`
--

LOCK TABLES `support_tickets` WRITE;
/*!40000 ALTER TABLE `support_tickets` DISABLE KEYS */;
INSERT INTO `support_tickets` VALUES (4001,30,NULL,'Amount not credited to wallet',NULL,'WALLET','HIGH','OPEN',NULL,NULL,NULL,'2025-11-29 06:47:24','2025-11-29 06:47:24');
/*!40000 ALTER TABLE `support_tickets` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `system_settings`
--

DROP TABLE IF EXISTS `system_settings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `system_settings` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `key` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `value` text COLLATE utf8mb4_unicode_ci,
  `description` text COLLATE utf8mb4_unicode_ci,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `key` (`key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `system_settings`
--

LOCK TABLES `system_settings` WRITE;
/*!40000 ALTER TABLE `system_settings` DISABLE KEYS */;
/*!40000 ALTER TABLE `system_settings` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `transaction_commissions`
--

DROP TABLE IF EXISTS `transaction_commissions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `transaction_commissions` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `pg_txn_id` bigint unsigned DEFAULT NULL,
  `bbps_txn_id` bigint unsigned DEFAULT NULL,
  `scheme_id` bigint unsigned DEFAULT NULL,
  `admin_amount` decimal(18,2) DEFAULT '0.00',
  `sd_user_id` bigint unsigned DEFAULT NULL,
  `sd_amount` decimal(18,2) DEFAULT '0.00',
  `distributor_user_id` bigint unsigned DEFAULT NULL,
  `distributor_amount` decimal(18,2) DEFAULT '0.00',
  `retailer_user_id` bigint unsigned DEFAULT NULL,
  `retailer_amount` decimal(18,2) DEFAULT '0.00',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `pg_txn_id` (`pg_txn_id`),
  KEY `bbps_txn_id` (`bbps_txn_id`),
  KEY `scheme_id` (`scheme_id`),
  KEY `sd_user_id` (`sd_user_id`),
  KEY `distributor_user_id` (`distributor_user_id`),
  KEY `retailer_user_id` (`retailer_user_id`),
  CONSTRAINT `transaction_commissions_ibfk_1` FOREIGN KEY (`pg_txn_id`) REFERENCES `pg_transactions` (`id`),
  CONSTRAINT `transaction_commissions_ibfk_2` FOREIGN KEY (`bbps_txn_id`) REFERENCES `bbps_transactions` (`id`),
  CONSTRAINT `transaction_commissions_ibfk_3` FOREIGN KEY (`scheme_id`) REFERENCES `commission_schemes` (`id`),
  CONSTRAINT `transaction_commissions_ibfk_4` FOREIGN KEY (`sd_user_id`) REFERENCES `users` (`id`),
  CONSTRAINT `transaction_commissions_ibfk_5` FOREIGN KEY (`distributor_user_id`) REFERENCES `users` (`id`),
  CONSTRAINT `transaction_commissions_ibfk_6` FOREIGN KEY (`retailer_user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `transaction_commissions`
--

LOCK TABLES `transaction_commissions` WRITE;
/*!40000 ALTER TABLE `transaction_commissions` DISABLE KEYS */;
INSERT INTO `transaction_commissions` VALUES (1,1001,NULL,1,27.50,10,5.50,20,2.50,30,19.50,'2025-11-29 06:45:53');
/*!40000 ALTER TABLE `transaction_commissions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `role_id` int unsigned NOT NULL,
  `parent_user_id` bigint unsigned DEFAULT NULL,
  `full_name` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `business_name` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `email` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `mobile` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `password_hash` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `status` enum('ACTIVE','SUSPENDED','KYC_PENDING','KYC_REJECTED') COLLATE utf8mb4_unicode_ci DEFAULT 'ACTIVE',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `TinNo` varchar(150) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `user_code` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `email` (`email`),
  UNIQUE KEY `mobile` (`mobile`),
  UNIQUE KEY `user_code` (`user_code`),
  KEY `role_id` (`role_id`),
  KEY `parent_user_id` (`parent_user_id`),
  CONSTRAINT `users_ibfk_1` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`),
  CONSTRAINT `users_ibfk_2` FOREIGN KEY (`parent_user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=48 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,1,NULL,'FINX Admin','FINX Network Pvt Ltd','admin@finx.com','9000000001','HASHED','ACTIVE','2025-11-29 06:13:20','2025-11-29 06:13:20',NULL,NULL),(10,2,1,'Srinivas SD','Srinivas Digital Services','sd1@finx.com','9000000002','HASHED','ACTIVE','2025-11-29 06:13:20','2025-11-29 06:13:20',NULL,NULL),(20,3,10,'Kumar Distributor','Kumar Payments','dist1@finx.com','9000000003','HASHED','ACTIVE','2025-11-29 06:13:20','2025-11-29 06:13:20',NULL,NULL),(30,4,20,'Ravi Retailer','Ravi BillPoint','ret1@finx.com','9000000004','HASHED','ACTIVE','2025-11-29 06:13:20','2025-11-29 06:13:20',NULL,NULL),(40,5,1,'Classic Travels','Classic Travels Pvt Ltd','mer1@finx.com','9000000005','HASHED','ACTIVE','2025-11-29 06:13:20','2025-11-29 06:13:20',NULL,NULL),(43,2,1,'test','asa','sss@gmail.com','12321321','$2a$11$PrHu5FGuRZNx6JJDip7KU.idTzlN/2xL2b838lDvWOwe.aP0NIf.C','ACTIVE','2025-12-01 19:36:05','2025-12-01 19:36:05','1111aa',NULL),(45,1,1,'qq','asdd','ssssss@gmail.com','12333','$2a$11$.KQpCGxbvOydC/cFbDRTDue0IkGaY52g6aMHqTqiZ6dQAThnswKRy','ACTIVE','2025-12-01 19:42:57','2025-12-01 19:42:57','zzxcx','ADM00044'),(47,3,1,'test','dasa','sdssa@gmail.com','8008521222','$2a$11$Nr9zjCH5zw6OoirVd/rw9uWyPdsxdvM92ldvhiRpnn/J9ZLUBvPIu','ACTIVE','2025-12-01 20:44:41','2025-12-01 20:44:41','123','DIS00046');
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `wallet_ledger`
--

DROP TABLE IF EXISTS `wallet_ledger`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `wallet_ledger` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `wallet_id` bigint unsigned NOT NULL,
  `txn_type` enum('CREDIT','DEBIT') COLLATE utf8mb4_unicode_ci NOT NULL,
  `source_type` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `source_id` bigint unsigned DEFAULT NULL,
  `amount` decimal(18,2) NOT NULL,
  `balance_before` decimal(18,2) DEFAULT NULL,
  `balance_after` decimal(18,2) DEFAULT NULL,
  `narration` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_walletledger_wallet_id` (`wallet_id`),
  CONSTRAINT `wallet_ledger_ibfk_1` FOREIGN KEY (`wallet_id`) REFERENCES `wallets` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `wallet_ledger`
--

LOCK TABLES `wallet_ledger` WRITE;
/*!40000 ALTER TABLE `wallet_ledger` DISABLE KEYS */;
/*!40000 ALTER TABLE `wallet_ledger` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `wallets`
--

DROP TABLE IF EXISTS `wallets`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `wallets` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `user_id` bigint unsigned NOT NULL,
  `currency` char(3) COLLATE utf8mb4_unicode_ci DEFAULT 'INR',
  `balance` decimal(18,2) NOT NULL DEFAULT '0.00',
  `hold_balance` decimal(18,2) NOT NULL DEFAULT '0.00',
  `status` enum('ACTIVE','BLOCKED') COLLATE utf8mb4_unicode_ci DEFAULT 'ACTIVE',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `user_id` (`user_id`),
  CONSTRAINT `wallets_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `wallets`
--

LOCK TABLES `wallets` WRITE;
/*!40000 ALTER TABLE `wallets` DISABLE KEYS */;
INSERT INTO `wallets` VALUES (1,1,'INR',500000.00,0.00,'ACTIVE','2025-11-29 06:19:50','2025-11-29 06:19:50'),(2,10,'INR',20000.00,0.00,'ACTIVE','2025-11-29 06:19:50','2025-11-29 06:19:50'),(3,20,'INR',8000.00,0.00,'ACTIVE','2025-11-29 06:19:50','2025-11-29 06:19:50'),(4,30,'INR',1200.00,0.00,'ACTIVE','2025-11-29 06:19:50','2025-11-29 06:19:50'),(5,40,'INR',0.00,0.00,'ACTIVE','2025-11-29 06:19:50','2025-11-29 06:19:50');
/*!40000 ALTER TABLE `wallets` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `webhook_logs`
--

DROP TABLE IF EXISTS `webhook_logs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `webhook_logs` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `provider_id` int unsigned DEFAULT NULL,
  `event_type` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `http_headers` json DEFAULT NULL,
  `payload` json DEFAULT NULL,
  `status` enum('RECEIVED','PROCESSED','FAILED','IGNORED') COLLATE utf8mb4_unicode_ci DEFAULT 'RECEIVED',
  `related_pg_txn_id` bigint unsigned DEFAULT NULL,
  `related_bbps_txn_id` bigint unsigned DEFAULT NULL,
  `related_payout_id` bigint unsigned DEFAULT NULL,
  `received_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `processed_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `provider_id` (`provider_id`),
  CONSTRAINT `webhook_logs_ibfk_1` FOREIGN KEY (`provider_id`) REFERENCES `providers` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `webhook_logs`
--

LOCK TABLES `webhook_logs` WRITE;
/*!40000 ALTER TABLE `webhook_logs` DISABLE KEYS */;
INSERT INTO `webhook_logs` VALUES (1,1,'payment.success',NULL,'{\"order_id\": \"CFX-TRAVEL-8989\"}','PROCESSED',1001,NULL,NULL,'2025-11-29 06:47:33',NULL);
/*!40000 ALTER TABLE `webhook_logs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping events for database 'payments_core'
--

--
-- Dumping routines for database 'payments_core'
--
/*!50003 DROP FUNCTION IF EXISTS `fn_generate_user_code` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` FUNCTION `fn_generate_user_code`(p_role_id INT) RETURNS varchar(50) CHARSET utf8mb4 COLLATE utf8mb4_unicode_ci
    DETERMINISTIC
BEGIN
    DECLARE prefix VARCHAR(3);
    DECLARE uid BIGINT;
    DECLARE role_name VARCHAR(100);

    SELECT name INTO role_name FROM roles WHERE id = p_role_id;
    SET prefix = UPPER(LEFT(role_name, 3));
    SET uid = (SELECT IFNULL(MAX(id),0) + 1 FROM users);

    RETURN CONCAT(prefix, LPAD(uid, 5, '0'));
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp1_user_get_profile` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp1_user_get_profile`(
    IN p_id BIGINT
)
BEGIN
    SELECT id, full_name, mobile, email, role_id, parent_user_id, business_name, tinno
    FROM users
    WHERE id = p_id
    LIMIT 1;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp1_user_login` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp1_user_login`(
    IN p_username VARCHAR(20)
)
BEGIN
    SELECT id, full_name, mobile, email, password_hash, role_id, parent_user_id, business_name, tinno
    FROM users
    WHERE user_code = p_username
    LIMIT 1;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp1_user_register` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp1_user_register`(
    IN p_full_name VARCHAR(200),
    IN p_mobile VARCHAR(20),
    IN p_email VARCHAR(200),
    IN p_password_hash VARCHAR(255),
    IN p_role_id INT,
    IN p_parent_user_id BIGINT,
    IN p_business_name VARCHAR(200),
    IN p_tin_no VARCHAR(50),
    OUT p_new_id BIGINT
)
BEGIN
    INSERT INTO users
        (user_code, full_name, mobile, email, password_hash, role_id, parent_user_id, business_name, TinNo)
    VALUES
        (
            fn_generate_user_code(p_role_id),   -- Auto-generate user_id like ADM00001
            p_full_name,
            p_mobile,
            p_email,
            p_password_hash,
            p_role_id,
            p_parent_user_id,
            p_business_name,
            p_tin_no
        );

    SET p_new_id = LAST_INSERT_ID();
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp1_user_update_profile` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp1_user_update_profile`(
    IN p_id BIGINT,
    IN p_full_name VARCHAR(200),
    IN p_business_name VARCHAR(200),
    IN p_tin_no VARCHAR(50)
)
BEGIN
    UPDATE users
    SET full_name = p_full_name,
        business_name = p_business_name,
        tin_no = p_tin_no
    WHERE id = p_id;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_add_kyc_document` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_add_kyc_document`(
    IN p_user_id BIGINT,
    IN p_doc_type VARCHAR(50),
    IN p_file_path VARCHAR(500)
)
BEGIN
    INSERT INTO kyc_documents(user_id, doc_type, file_path, status)
    VALUES(p_user_id, p_doc_type, p_file_path, 'PENDING');
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_add_settlement_item` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_add_settlement_item`(
  IN p_batch_id BIGINT,
  IN p_pg_txn_id BIGINT,
  IN p_bbps_txn_id BIGINT,
  IN p_amount DECIMAL(18,2),
  IN p_fee_amount DECIMAL(18,2)
)
BEGIN
  INSERT INTO settlement_items (batch_id, pg_txn_id, bbps_txn_id, amount, fee_amount, created_at)
    VALUES (p_batch_id, p_pg_txn_id, p_bbps_txn_id, p_amount, p_fee_amount, NOW());
  -- update totals in batch
  UPDATE settlement_batches SET total_amount = COALESCE(total_amount,0) + p_amount, total_fee = COALESCE(total_fee,0) + COALESCE(p_fee_amount,0) WHERE id = p_batch_id;
  SELECT 'ITEM_ADDED' AS status, LAST_INSERT_ID() AS item_id;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_apply_commission_scheme` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_apply_commission_scheme`(
  IN p_txn_type VARCHAR(10),  -- 'PG' or 'BBPS'
  IN p_txn_id BIGINT,
  IN p_scheme_id BIGINT
)
BEGIN
  DECLARE v_amount DECIMAL(18,2);
  DECLARE v_admin_pct DECIMAL(5,2);
  DECLARE v_sd_pct DECIMAL(5,2);
  DECLARE v_dist_pct DECIMAL(5,2);
  DECLARE v_retailer_pct DECIMAL(5,2);
  DECLARE v_admin_amt DECIMAL(18,2);
  DECLARE v_sd_amt DECIMAL(18,2);
  DECLARE v_dist_amt DECIMAL(18,2);
  DECLARE v_retailer_amt DECIMAL(18,2);
  DECLARE v_retailer_id BIGINT;
  DECLARE v_dist_id BIGINT;
  DECLARE v_sd_id BIGINT;

  IF p_txn_type = 'PG' THEN
    SELECT amount, merchant_id INTO v_amount, v_retailer_id FROM pg_transactions WHERE id = p_txn_id;
  ELSE
    SELECT amount, created_by_user INTO v_amount, v_retailer_id FROM bbps_transactions WHERE id = p_txn_id;
  END IF;

  SELECT admin_percent, sd_percent, distributor_percent, retailer_percent
    INTO v_admin_pct, v_sd_pct, v_dist_pct, v_retailer_pct
  FROM commission_schemes WHERE id = p_scheme_id FOR UPDATE;

  SET v_admin_amt = ROUND((v_amount * v_admin_pct)/100, 2);
  SET v_sd_amt = ROUND((v_amount * v_sd_pct)/100, 2);
  SET v_dist_amt = ROUND((v_amount * v_dist_pct)/100, 2);
  SET v_retailer_amt = ROUND((v_amount * v_retailer_pct)/100, 2);

  -- Simple assignment: use parent hierarchy to locate sd/dist (this is sample logic; adapt to your hierarchy)
  SELECT parent_user_id INTO v_dist_id FROM users WHERE id = v_retailer_id;
  SELECT parent_user_id INTO v_sd_id FROM users WHERE id = v_dist_id;

  INSERT INTO transaction_commissions (pg_txn_id, bbps_txn_id, scheme_id, admin_amount, sd_user_id, sd_amount, distributor_user_id, distributor_amount, retailer_user_id, retailer_amount, created_at)
    VALUES (
      CASE WHEN p_txn_type='PG' THEN p_txn_id ELSE NULL END,
      CASE WHEN p_txn_type='BBPS' THEN p_txn_id ELSE NULL END,
      p_scheme_id,
      v_admin_amt, v_sd_id, v_sd_amt, v_dist_id, v_dist_amt, v_retailer_id, v_retailer_amt, NOW()
    );

  SELECT 'COMMISSION_INSERTED' AS status, LAST_INSERT_ID() AS commission_id;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_bbps_finalize_success` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_bbps_finalize_success`(
  IN p_bbps_txn_id BIGINT
)
BEGIN
  DECLARE v_amount DECIMAL(18,2);
  DECLARE v_biller_id BIGINT;
  DECLARE v_provider_id INT;

  START TRANSACTION;

  SELECT amount, biller_id, provider_id INTO v_amount, v_biller_id, v_provider_id
    FROM bbps_transactions
    WHERE id = p_bbps_txn_id
    FOR UPDATE;

  -- Create a transaction_commissions row using default scheme (choose appropriate scheme_id)
  INSERT INTO transaction_commissions (bbps_txn_id, scheme_id, admin_amount, sd_user_id, sd_amount, distributor_user_id, distributor_amount, retailer_user_id, retailer_amount, created_at)
    VALUES (p_bbps_txn_id, (SELECT id FROM commission_schemes WHERE name = 'Default PG Commission' LIMIT 1), 0, NULL, 0, NULL, 0, NULL, 0, NOW());

  -- Log finalize event
  INSERT INTO webhook_logs(provider_id, event_type, payload, status, related_bbps_txn_id, received_at)
    VALUES (v_provider_id, 'bbps.finalize', JSON_OBJECT('bbps_txn_id', p_bbps_txn_id, 'amount', v_amount), 'PROCESSED', p_bbps_txn_id, NOW());

  COMMIT;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_bbps_handle_callback` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_bbps_handle_callback`(
  IN p_bbps_txn_id BIGINT,
  IN p_provider_tx_ref VARCHAR(128),
  IN p_status ENUM('PENDING','SUCCESS','FAILED','REVERSED'),
  IN p_receipt_json JSON
)
BEGIN

  bbps_cb_block: BEGIN

    DECLARE v_old_status VARCHAR(20);
    DECLARE v_user BIGINT;
    DECLARE v_amount DECIMAL(18,2);

    START TRANSACTION;

    SELECT status, created_by_user, amount
      INTO v_old_status, v_user, v_amount
    FROM bbps_transactions
    WHERE id = p_bbps_txn_id
    FOR UPDATE;

    -- Idempotent, same status
    IF v_old_status = p_status THEN

      UPDATE bbps_transactions
      SET
        receipt_data = JSON_MERGE_PRESERVE(
                          IFNULL(receipt_data, JSON_OBJECT()),
                          p_receipt_json
                        ),
        completed_at = CASE WHEN p_status='SUCCESS'
                            THEN NOW() ELSE completed_at END
      WHERE id = p_bbps_txn_id;

      COMMIT;

      SELECT 'IGNORED_SAME_STATUS' AS status;
      LEAVE bbps_cb_block;
    END IF;


    -- Update status and receipt
    UPDATE bbps_transactions
      SET status = p_status,
          bbps_ref = COALESCE(bbps_ref, p_provider_tx_ref),
          receipt_data = JSON_MERGE_PRESERVE(
                            IFNULL(receipt_data, JSON_OBJECT()),
                            p_receipt_json
                         ),
          completed_at = NOW()
    WHERE id = p_bbps_txn_id;


    -- SUCCESS FLOW
    IF p_status = 'SUCCESS' THEN

      CALL sp_bbps_finalize_success(p_bbps_txn_id);

    ELSE

      -- FAILED / REVERSED: refund from hold
      CALL sp_wallet_credit_secure(
          v_user,
          v_amount,
          'BBPS_REFUND',
          p_bbps_txn_id,
          CONCAT('BBPS_REV_', p_bbps_txn_id)
      );

      INSERT INTO webhook_logs(
        provider_id,
        event_type,
        payload,
        status,
        related_bbps_txn_id,
        received_at
      )
      VALUES (
        (SELECT provider_id FROM bbps_transactions WHERE id = p_bbps_txn_id),
        CONCAT('bbps.callback.', p_status),
        p_receipt_json,
        'PROCESSED',
        p_bbps_txn_id,
        NOW()
      );

    END IF;

    COMMIT;

  END bbps_cb_block;

END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_bbps_initiate` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_bbps_initiate`(
  IN p_request_tag VARCHAR(128),
  IN p_provider_id INT,
  IN p_biller_id BIGINT,
  IN p_created_by_user BIGINT,
  IN p_biller_customer_ref VARCHAR(128),
  IN p_amount DECIMAL(18,2)
)
BEGIN

  bbps_init_block: BEGIN

    DECLARE v_exists INT DEFAULT 0;
    DECLARE v_bbps_txn_id BIGINT;

    -- Duplicate prevention
    SELECT COUNT(1)
      INTO v_exists
    FROM bbps_transactions
    WHERE customer_ref = p_biller_customer_ref
      AND biller_id = p_biller_id
      AND amount = p_amount
      AND created_by_user = p_created_by_user
      AND status IN ('PENDING','SUCCESS');

    IF v_exists > 0 THEN
      SELECT 'DUPLICATE_PROBABLE' AS status,
             'Possible duplicate request' AS message;
      LEAVE bbps_init_block;
    END IF;

    START TRANSACTION;

    -- Insert BBPS transaction
    INSERT INTO bbps_transactions (
      provider_id,
      bbps_ref,
      pg_txn_id,
      created_by_user,
      biller_id,
      customer_ref,
      amount,
      status,
      initiated_at
    ) VALUES (
      p_provider_id,
      NULL,
      NULL,
      p_created_by_user,
      p_biller_id,
      p_biller_customer_ref,
      p_amount,
      'PENDING',
      NOW()
    );

    SET v_bbps_txn_id = LAST_INSERT_ID();

    -- Wallet HOLD debit
    CALL sp_wallet_debit_secure(
      p_created_by_user,
      p_amount,
      'BBPS_HOLD',
      v_bbps_txn_id,
      CONCAT('BBPS_HOLD_', v_bbps_txn_id)
    );

    -- Add webhook event
    INSERT INTO webhook_logs(
      provider_id,
      event_type,
      payload,
      status,
      related_bbps_txn_id,
      received_at
    )
    VALUES (
      p_provider_id,
      'bbps.initiated',
      JSON_OBJECT('bbps_txn_id', v_bbps_txn_id),
      'RECEIVED',
      v_bbps_txn_id,
      NOW()
    );

    COMMIT;

    SELECT 'BBPS_CREATED' AS status,
           v_bbps_txn_id AS bbps_txn_id;

  END bbps_init_block;

END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_create_merchant` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_create_merchant`(
    IN p_user_id BIGINT,
    IN p_legal_name VARCHAR(255),
    IN p_trade_name VARCHAR(255),
    IN p_business_type VARCHAR(50),
    IN p_category VARCHAR(50),
    IN p_website_url VARCHAR(255)
)
BEGIN
    INSERT INTO merchants (
        user_id, legal_name, trade_name, business_type,
        category, website_url, kyc_status
    )
    VALUES (
        p_user_id, p_legal_name, p_trade_name, p_business_type,
        p_category, p_website_url, 'PENDING'
    );
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_create_role` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_create_role`(
    IN p_name VARCHAR(50),
    IN p_description VARCHAR(255)
)
BEGIN
    INSERT INTO roles(name, description)
    VALUES(p_name, p_description);
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_create_settlement_batch` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_create_settlement_batch`(
  IN p_user_id BIGINT,
  IN p_batch_ref VARCHAR(128)
)
BEGIN
  INSERT INTO settlement_batches (batch_ref, user_id, total_amount, total_fee, settlement_mode, status, created_at)
    VALUES (p_batch_ref, p_user_id, 0, 0, 'AUTO', 'CREATED', NOW());
  SELECT 'BATCH_CREATED' AS status, LAST_INSERT_ID() AS batch_id;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_create_user` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_create_user`(
    IN p_role_id INT,
    IN p_parent_user_id BIGINT,
    IN p_full_name VARCHAR(200),
    IN p_business_name VARCHAR(200),
    IN p_email VARCHAR(255),
    IN p_mobile VARCHAR(20),
    IN p_password_hash VARCHAR(255)
)
BEGIN
    INSERT INTO users (
        role_id, parent_user_id, full_name, business_name,
        email, mobile, password_hash, status
    )
    VALUES (
        p_role_id, p_parent_user_id, p_full_name, p_business_name,
        p_email, p_mobile, p_password_hash, 'ACTIVE'
    );
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_credit_commission_wallets` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_credit_commission_wallets`(
  IN p_commission_id BIGINT
)
BEGIN
  DECLARE v_sd_user BIGINT;
  DECLARE v_sd_amt DECIMAL(18,2);
  DECLARE v_dist_user BIGINT;
  DECLARE v_dist_amt DECIMAL(18,2);
  DECLARE v_retailer_user BIGINT;
  DECLARE v_retailer_amt DECIMAL(18,2);

  SELECT sd_user_id, sd_amount, distributor_user_id, distributor_amount, retailer_user_id, retailer_amount
    INTO v_sd_user, v_sd_amt, v_dist_user, v_dist_amt, v_retailer_user, v_retailer_amt
  FROM transaction_commissions WHERE id = p_commission_id FOR UPDATE;

  -- credit sd
  IF v_sd_user IS NOT NULL AND v_sd_amt > 0 THEN
    CALL sp_wallet_commission_credit(p_commission_id, v_sd_user, v_sd_amt);
  END IF;

  -- credit distributor
  IF v_dist_user IS NOT NULL AND v_dist_amt > 0 THEN
    CALL sp_wallet_commission_credit(p_commission_id, v_dist_user, v_dist_amt);
  END IF;

  -- credit retailer
  IF v_retailer_user IS NOT NULL AND v_retailer_amt > 0 THEN
    CALL sp_wallet_commission_credit(p_commission_id, v_retailer_user, v_retailer_amt);
  END IF;

  UPDATE transaction_commissions SET created_at = created_at WHERE id = p_commission_id; -- noop to avoid untouched result
  SELECT 'COMMISSION_CREDITED' AS status;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_master_get_billers` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_master_get_billers`(IN p_category VARCHAR(100))
BEGIN
  IF p_category IS NULL OR p_category = '' THEN
    SELECT id, biller_code AS BillerCode, bank_name AS BillerName, Category, status AS IsActive FROM bbps_billers;
  ELSE
    SELECT id, biller_code AS billerCode, bank_name AS BillerName, Category, status AS IsActive 
    FROM bbps_billers WHERE category = p_category;
  END IF;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_master_get_mdr_pricing` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_master_get_mdr_pricing`()
BEGIN
  SELECT id, category AS productType, slab_min_amount AS minAmount, slab_max_amount AS maxAmount, mdr_percent AS mdrPercent, 
  fixed_fee AS mdrFixed FROM mdr_pricing;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_master_get_providers` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_master_get_providers`()
BEGIN
  SELECT id, code AS ProviderCode, name AS ProviderName,type ProviderType, status AS isActive FROM providers;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_payout_handle_callback` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_payout_handle_callback`(
  IN p_payout_id BIGINT,
  IN p_provider_ref VARCHAR(128),
  IN p_status ENUM('INITIATED','PENDING','SUCCESS','FAILED','REVERSED'),
  IN p_callback_payload JSON
)
payout_cb_end: BEGIN

  DECLARE v_old_status VARCHAR(20);
  DECLARE v_user BIGINT;
  DECLARE v_wallet BIGINT;
  DECLARE v_amount DECIMAL(18,2);
  DECLARE v_fee DECIMAL(18,2);

  START TRANSACTION;

  SELECT status, user_id, wallet_id, amount, fee_amount
    INTO v_old_status, v_user, v_wallet, v_amount, v_fee
    FROM payouts
    WHERE id = p_payout_id
    FOR UPDATE;

  -- If same status → ignore callback
  IF v_old_status = p_status THEN
    UPDATE payouts
      SET provider_ref = COALESCE(provider_ref, p_provider_ref),
          completed_at = CASE WHEN p_status='SUCCESS' THEN NOW() ELSE completed_at END
      WHERE id = p_payout_id;

    COMMIT;

    SELECT 'IGNORED_SAME_STATUS' AS status;
    LEAVE payout_cb_end;
  END IF;

  UPDATE payouts
    SET status = p_status,
        provider_ref = COALESCE(provider_ref, p_provider_ref),
        completed_at = NOW()
  WHERE id = p_payout_id;

  IF p_status = 'SUCCESS' THEN

    INSERT INTO webhook_logs(provider_id, event_type, payload, status, related_payout_id, received_at)
      VALUES (
        (SELECT provider_id FROM payouts WHERE id = p_payout_id),
        'payout.success',
        p_callback_payload,
        'PROCESSED',
        p_payout_id,
        NOW()
      );

  ELSE
    -- FAILED or REVERSED → Refund wallet
    CALL sp_wallet_credit_secure(
        v_user,
        (v_amount + IFNULL(v_fee,0)),
        'PAYOUT_REVERSAL',
        p_payout_id,
        CONCAT('PAYOUT_REV_', p_payout_id)
    );

    INSERT INTO webhook_logs(provider_id, event_type, payload, status, related_payout_id, received_at)
      VALUES (
        (SELECT provider_id FROM payouts WHERE id = p_payout_id),
        CONCAT('payout.', p_status),
        p_callback_payload,
        'PROCESSED',
        p_payout_id,
        NOW()
      );
  END IF;

  COMMIT;

END payout_cb_end ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_payout_initiate` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_payout_initiate`(
  IN p_user_id BIGINT,
  IN p_wallet_id BIGINT,
  IN p_provider_id INT,
  IN p_amount DECIMAL(18,2),
  IN p_fee_amount DECIMAL(18,2),
  IN p_mode ENUM('IMPS','NEFT','UPI','BANK_TRANSFER'),
  IN p_beneficiary_name VARCHAR(255),
  IN p_beneficiary_account_no VARCHAR(64),
  IN p_beneficiary_ifsc VARCHAR(20),
  IN p_beneficiary_vpa VARCHAR(128)
)
BEGIN
  DECLARE v_payout_id BIGINT;
  DECLARE v_net DECIMAL(18,2);
  DECLARE v_status VARCHAR(20);

  SET v_net = p_amount + IFNULL(p_fee_amount,0);

  START TRANSACTION;

  INSERT INTO payouts (provider_id, wallet_id, user_id, amount, fee_amount, mode, beneficiary_name, beneficiary_account_no, beneficiary_ifsc, beneficiary_vpa, status, initiated_at)
    VALUES (p_provider_id, p_wallet_id, p_user_id, p_amount, p_fee_amount, p_mode, p_beneficiary_name, p_beneficiary_account_no, p_beneficiary_ifsc, p_beneficiary_vpa, 'INITIATED', NOW());
  SET v_payout_id = LAST_INSERT_ID();

  -- debit wallet for amount + fee (use secure debit with idempotency)
  CALL sp_wallet_debit_secure(p_user_id, v_net, 'PAYOUT', v_payout_id, CONCAT('PAYOUT_INIT_', v_payout_id));

  -- log webhook to call provider (external system should pick this payout record and call the provider API)
  INSERT INTO webhook_logs(provider_id, event_type, payload, status, related_payout_id, received_at)
    VALUES (p_provider_id, 'payout.initiated', JSON_OBJECT('payout_id', v_payout_id), 'RECEIVED', NULL, NOW());

  COMMIT;
  SELECT 'PAYOUT_CREATED' AS status, v_payout_id AS payout_id;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_pg_finalize_success` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_pg_finalize_success`(
  IN p_pg_txn_id BIGINT
)
BEGIN
  finalize_block: BEGIN

    DECLARE v_provider_id INT;
    DECLARE v_amount DECIMAL(18,2);
    DECLARE v_category VARCHAR(50);
    DECLARE v_mdr_percent DECIMAL(6,4) DEFAULT 0.0000;
    DECLARE v_mdr_amount DECIMAL(18,2) DEFAULT 0.00;
    DECLARE v_provider_fee DECIMAL(18,2) DEFAULT 0.00;
    DECLARE v_platform_margin DECIMAL(18,2) DEFAULT 0.00;
    DECLARE v_created_by_user BIGINT;
    DECLARE v_merchant_id BIGINT;

    START TRANSACTION;

    -- Lock the PG transaction row
    SELECT provider_id, amount, category, created_by_user, merchant_id
      INTO v_provider_id, v_amount, v_category, v_created_by_user, v_merchant_id
    FROM pg_transactions
    WHERE id = p_pg_txn_id
    FOR UPDATE;

    -- if not found
    IF v_provider_id IS NULL THEN
      ROLLBACK;
      SELECT 'TXN_NOT_FOUND' AS status;
      LEAVE finalize_block;
    END IF;

    -- Try fetch MDR
    SELECT mdr_percent, fixed_fee
      INTO v_mdr_percent, v_provider_fee
    FROM mdr_pricing
    WHERE provider_id = v_provider_id
      AND category = v_category
    ORDER BY id LIMIT 1;

    -- fallback if empty result
    IF v_mdr_percent IS NULL THEN
      SET v_mdr_percent = 1.00;  -- 1%
      SET v_provider_fee = 0.00;
    END IF;

    SET v_mdr_amount = ROUND((v_amount * v_mdr_percent) / 100.0, 2);
    SET v_platform_margin = ROUND(GREATEST(v_mdr_amount - IFNULL(v_provider_fee,0), 0), 2);

    -- Update PG transaction
    UPDATE pg_transactions
      SET mdr_percent = v_mdr_percent,
          mdr_amount = v_mdr_amount,
          provider_fee = v_provider_fee,
          finx_margin = v_platform_margin
    WHERE id = p_pg_txn_id;

    -- Wallet load credit
    IF v_category = 'WALLET_LOAD' THEN
      CALL sp_wallet_credit_secure(
        v_created_by_user,
        v_amount,
        'PG_TXN',
        p_pg_txn_id,
        CONCAT('PGLOAD_', p_pg_txn_id)
      );
    END IF;

    -- Insert webhook record
    INSERT INTO webhook_logs(provider_id, event_type, payload, status, related_pg_txn_id, received_at)
    VALUES (
      v_provider_id,
      'pg_finalize_success',
      JSON_OBJECT('pg_txn_id', p_pg_txn_id, 'mdr', v_mdr_amount),
      'PROCESSED',
      p_pg_txn_id,
      NOW()
    );

    COMMIT;

  END finalize_block;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_pg_handle_callback` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_pg_handle_callback`(
  IN p_pg_txn_request_id VARCHAR(128),
  IN p_provider_tx_ref VARCHAR(128),
  IN p_status ENUM('INITIATED','PENDING','SUCCESS','FAILED','REFUNDED','CHARGEBACK'),
  IN p_provider_payload JSON
)
proc_end:   -- <<< ADD LABEL HERE
BEGIN
  DECLARE v_txn_id BIGINT;
  DECLARE v_old_status VARCHAR(20);
  DECLARE v_provider_id INT;
  DECLARE v_amount DECIMAL(18,2);

  START TRANSACTION;

  -- find transaction by request_id
  SELECT id, status, provider_id, amount
    INTO v_txn_id, v_old_status, v_provider_id, v_amount
  FROM pg_transactions
  WHERE request_id = p_pg_txn_request_id
  FOR UPDATE;

  IF v_txn_id IS NULL THEN
    -- try match by provider external ref
    SELECT id, status, provider_id, amount
      INTO v_txn_id, v_old_status, v_provider_id, v_amount
    FROM pg_transactions
    WHERE external_ref = p_provider_tx_ref
    FOR UPDATE;
  END IF;

  IF v_txn_id IS NULL THEN
    INSERT INTO webhook_logs(provider_id, event_type, payload, status, related_pg_txn_id, received_at)
      VALUES (NULL, 'pg.callback.unknown', p_provider_payload, 'RECEIVED', NULL, NOW());
    COMMIT;
    SELECT 'UNKNOWN_TXN' AS status;
    LEAVE proc_end;   -- <<< Now valid
  END IF;

  -- Idempotency check
  IF v_old_status = p_status THEN
    UPDATE pg_transactions
      SET provider_payload = JSON_MERGE_PRESERVE(IFNULL(provider_payload, JSON_OBJECT()), p_provider_payload),
          external_ref = COALESCE(external_ref, p_provider_tx_ref),
          completed_at = CASE WHEN p_status = 'SUCCESS' THEN NOW() ELSE completed_at END
     WHERE id = v_txn_id;

    COMMIT;
    SELECT 'IGNORED_SAME_STATUS' AS status;
    LEAVE proc_end;   -- <<< Now valid
  END IF;

  -- update
  UPDATE pg_transactions
     SET status = p_status,
         external_ref = COALESCE(external_ref, p_provider_tx_ref),
         provider_payload = JSON_MERGE_PRESERVE(IFNULL(provider_payload, JSON_OBJECT()), p_provider_payload),
         completed_at = CASE WHEN p_status IN ('SUCCESS','FAILED','REFUNDED','CHARGEBACK') THEN NOW() ELSE NULL END
   WHERE id = v_txn_id;

  -- process based on status
  IF p_status = 'SUCCESS' THEN
      CALL sp_pg_finalize_success(v_txn_id);
  ELSEIF p_status = 'FAILED' THEN
      INSERT INTO webhook_logs(provider_id, event_type, payload, status, related_pg_txn_id, received_at)
        VALUES (v_provider_id, CONCAT('pg.callback.', p_status), p_provider_payload, 'PROCESSED', v_txn_id, NOW());
  END IF;

  COMMIT;

END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_pg_initiate` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_pg_initiate`(
  IN p_request_id VARCHAR(128),
  IN p_provider_id INT,
  IN p_category VARCHAR(50),
  IN p_created_by_user BIGINT,
  IN p_merchant_id BIGINT,
  IN p_amount DECIMAL(18,2),
  IN p_currency CHAR(3),
  IN p_callback_url VARCHAR(500)
)
BEGIN
  DECLARE v_exists INT DEFAULT 0;
  SELECT COUNT(1) INTO v_exists FROM pg_transactions WHERE request_id = p_request_id;
  IF v_exists > 0 THEN
    SELECT 'DUPLICATE_REQUEST' AS status, 'Request already exists' AS message;
  ELSE
    INSERT INTO pg_transactions (
      external_ref, provider_id, category, created_by_user, merchant_id,
      amount, currency, status, request_id, callback_url, initiated_at
    ) VALUES (
      NULL, p_provider_id, p_category, p_created_by_user, p_merchant_id,
      p_amount, IFNULL(p_currency, 'INR'), 'INITIATED', p_request_id, p_callback_url, NOW()
    );
    SELECT 'CREATED' AS status, LAST_INSERT_ID() AS pg_txn_id;
  END IF;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_process_settlement_batch` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_process_settlement_batch`(
  IN p_batch_id BIGINT
)
BEGIN
  DECLARE done INT DEFAULT 0;
  DECLARE v_item_id BIGINT;
  DECLARE v_pg_txn_id BIGINT;
  DECLARE v_bbps_txn_id BIGINT;
  DECLARE v_amount DECIMAL(18,2);
  DECLARE item_cursor CURSOR FOR SELECT id, pg_txn_id, bbps_txn_id, amount FROM settlement_items WHERE batch_id = p_batch_id;
  DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = 1;

  START TRANSACTION;

  UPDATE settlement_batches SET status = 'PROCESSING', completed_at = NULL WHERE id = p_batch_id;

  OPEN item_cursor;
  read_loop: LOOP
    FETCH item_cursor INTO v_item_id, v_pg_txn_id, v_bbps_txn_id, v_amount;
    IF done THEN
      LEAVE read_loop;
    END IF;

    -- determine user for the txn (use pg_transactions.merchant_id or bbps_transactions.created_by_user)
    IF v_pg_txn_id IS NOT NULL THEN
      CALL sp_payout_initiate(
        (SELECT merchant_id FROM pg_transactions WHERE id = v_pg_txn_id),
        (SELECT id FROM wallets WHERE user_id = (SELECT merchant_id FROM pg_transactions WHERE id = v_pg_txn_id) LIMIT 1),
        (SELECT provider_id FROM pg_transactions WHERE id = v_pg_txn_id),
        v_amount,
        0.00, -- fee
        'IMPS',
        'Settlement Payout',
        'BANK_ACC_PLACEHOLDER',
        'IFSC0000',
        NULL
      );
    ELSEIF v_bbps_txn_id IS NOT NULL THEN
      CALL sp_payout_initiate(
        (SELECT created_by_user FROM bbps_transactions WHERE id = v_bbps_txn_id),
        (SELECT id FROM wallets WHERE user_id = (SELECT created_by_user FROM bbps_transactions WHERE id = v_bbps_txn_id) LIMIT 1),
        (SELECT provider_id FROM bbps_transactions WHERE id = v_bbps_txn_id),
        v_amount,
        0.00,
        'IMPS',
        'Settlement Payout',
        'BANK_ACC_PLACEHOLDER',
        'IFSC0000',
        NULL
      );
    END IF;

    -- optionally mark settlement_item processed (we'll leave it for callbacks to mark)
  END LOOP;

  CLOSE item_cursor;

  UPDATE settlement_batches SET status = 'COMPLETED', completed_at = NOW() WHERE id = p_batch_id;

  COMMIT;
  SELECT 'BATCH_PROCESSED' AS status, p_batch_id AS batch_id;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_record_webhook` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_record_webhook`(
  IN p_provider_id INT,
  IN p_event_type VARCHAR(100),
  IN p_headers JSON,
  IN p_payload JSON,
  IN p_related_pg_txn_id BIGINT,
  IN p_related_bbps_txn_id BIGINT,
  IN p_related_payout_id BIGINT
)
BEGIN
  INSERT INTO webhook_logs(
    provider_id, event_type, http_headers, payload, status,
    related_pg_txn_id, related_bbps_txn_id, related_payout_id, received_at
  ) VALUES (
    p_provider_id, p_event_type, p_headers, p_payload, 'RECEIVED',
    p_related_pg_txn_id, p_related_bbps_txn_id, p_related_payout_id, NOW()
  );
  SELECT 'OK' AS status, LAST_INSERT_ID() AS webhook_id;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_roles_get_all` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_roles_get_all`()
BEGIN
    SELECT id RoleID, name RoleName
    FROM roles
    ORDER BY id;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_save_login_otp` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_save_login_otp`(
    IN p_user_id BIGINT,
    IN p_mobile VARCHAR(20),
    IN p_otp VARCHAR(6)
)
BEGIN
    INSERT INTO login_otps (user_id, mobile, otp, created_at, is_used)
    VALUES (p_user_id, p_mobile, p_otp, NOW(), 0);
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_upsert_kyc_profile` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_upsert_kyc_profile`(
    IN p_user_id BIGINT,
    IN p_pan VARCHAR(20),
    IN p_gstin VARCHAR(20),
    IN p_addr1 VARCHAR(255),
    IN p_city VARCHAR(100),
    IN p_state VARCHAR(100),
    IN p_pincode VARCHAR(20),
    IN p_account VARCHAR(50),
    IN p_ifsc VARCHAR(20)
)
BEGIN
    INSERT INTO kyc_profiles (
        user_id, pan_number, gstin, address_line1,
        city, state, pincode, bank_account_no, bank_ifsc
    )
    VALUES (
        p_user_id, p_pan, p_gstin, p_addr1,
        p_city, p_state, p_pincode, p_account, p_ifsc
    )
    ON DUPLICATE KEY UPDATE
        pan_number = p_pan,
        gstin = p_gstin,
        address_line1 = p_addr1,
        city = p_city,
        state = p_state,
        pincode = p_pincode,
        bank_account_no = p_account,
        bank_ifsc = p_ifsc;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_verify_login_otp` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_verify_login_otp`(
    IN p_user_id BIGINT,
    IN p_otp VARCHAR(6)
)
BEGIN
    DECLARE v_count INT;

    SELECT COUNT(*) INTO v_count
    FROM login_otps
    WHERE user_id = p_user_id
      AND otp = p_otp
      AND is_used = 0
      AND created_at >= NOW() - INTERVAL 10 MINUTE;

    IF v_count = 1 THEN
        UPDATE login_otps SET is_used = 1
        WHERE user_id = p_user_id AND otp = p_otp;

        SELECT 1;
    ELSE
        SELECT 0;
    END IF;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_wallet_bbps_pay` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_wallet_bbps_pay`(
    IN p_bbps_txn_id BIGINT
)
BEGIN
    DECLARE v_user BIGINT;
    DECLARE v_amount DECIMAL(18,2);

    SELECT created_by_user, amount 
    INTO v_user, v_amount
    FROM bbps_transactions
    WHERE id = p_bbps_txn_id;

    CALL sp_wallet_debit_secure(
        v_user,
        v_amount,
        'BBPS_TXN',
        p_bbps_txn_id,
        CONCAT('BBPS_PAY_', p_bbps_txn_id)
    );
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_wallet_commission_credit` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_wallet_commission_credit`(
    IN p_comm_id BIGINT,
    IN p_user_id BIGINT,
    IN p_amount DECIMAL(18,2)
)
BEGIN
    CALL sp_wallet_credit_secure(
        p_user_id,
        p_amount,
        'COMMISSION',
        p_comm_id,
        CONCAT('COMM_', p_comm_id, '_U_', p_user_id)
    );
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_wallet_credit_secure` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_wallet_credit_secure`(
    IN p_user_id BIGINT,
    IN p_amount DECIMAL(18,2),
    IN p_source_type VARCHAR(50),
    IN p_source_id BIGINT,
    IN p_idempotency_key VARCHAR(128)
)
main_block: BEGIN
    DECLARE v_wallet_id BIGINT;
    DECLARE v_balance_before DECIMAL(18,2);
    DECLARE v_balance_after DECIMAL(18,2);

    -- Start atomic block
    START TRANSACTION;

    -- 1. Prevent duplicate credit
    IF EXISTS (
        SELECT 1 FROM wallet_ledger 
        WHERE source_type = p_source_type 
          AND source_id = p_source_id
          AND narration = p_idempotency_key
    ) THEN
        ROLLBACK;
        SELECT 'ALREADY_PROCESSED' AS status;
        LEAVE main_block;
    END IF;

    -- 2. Get wallet and lock
    SELECT id, balance 
    INTO v_wallet_id, v_balance_before
    FROM wallets
    WHERE user_id = p_user_id
    FOR UPDATE;

    -- 3. Update balance
    SET v_balance_after = v_balance_before + p_amount;

    UPDATE wallets
    SET balance = v_balance_after,
        updated_at = NOW()
    WHERE id = v_wallet_id;

    -- 4. Insert ledger entry
    INSERT INTO wallet_ledger (
        wallet_id, txn_type, source_type, source_id,
        amount, balance_before, balance_after, narration
    ) VALUES (
        v_wallet_id, 'CREDIT', p_source_type, p_source_id,
        p_amount, v_balance_before, v_balance_after, p_idempotency_key
    );

    -- 5. Commit
    COMMIT;

    SELECT 'SUCCESS' AS status, v_balance_after AS new_balance;

END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_wallet_debit_secure` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_wallet_debit_secure`(
    IN p_user_id BIGINT,
    IN p_amount DECIMAL(18,2),
    IN p_source_type VARCHAR(50),
    IN p_source_id BIGINT,
    IN p_idempotency_key VARCHAR(128)
)
main_block: BEGIN
    DECLARE v_wallet_id BIGINT;
    DECLARE v_balance_before DECIMAL(18,2);
    DECLARE v_balance_after DECIMAL(18,2);

    START TRANSACTION;

    -- 1. Prevent duplicate debit
    IF EXISTS (
        SELECT 1 FROM wallet_ledger 
        WHERE source_type = p_source_type 
          AND source_id = p_source_id
          AND narration = p_idempotency_key
          AND txn_type = 'DEBIT'
    ) THEN
        ROLLBACK;
        SELECT 'ALREADY_PROCESSED' AS status;
        LEAVE main_block;
    END IF;

    -- 2. Lock wallet
    SELECT id, balance 
    INTO v_wallet_id, v_balance_before
    FROM wallets
    WHERE user_id = p_user_id
    FOR UPDATE;

    -- 3. Negative Balance Protection
    IF v_balance_before < p_amount THEN
        ROLLBACK;
        SELECT 'INSUFFICIENT_BALANCE' AS status;
        LEAVE main_block;
    END IF;

    -- 4. Compute new balance
    SET v_balance_after = v_balance_before - p_amount;

    UPDATE wallets
    SET balance = v_balance_after,
        updated_at = NOW()
    WHERE id = v_wallet_id;

    -- 5. Ledger Entry
    INSERT INTO wallet_ledger (
        wallet_id, txn_type, source_type, source_id,
        amount, balance_before, balance_after, narration
    ) VALUES (
        v_wallet_id, 'DEBIT', p_source_type, p_source_id,
        p_amount, v_balance_before, v_balance_after, p_idempotency_key
    );

    COMMIT;

    SELECT 'SUCCESS' AS status, v_balance_after AS new_balance;

END main_block ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_wallet_payout_debit` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_wallet_payout_debit`(
    IN p_payout_id BIGINT
)
BEGIN
    DECLARE v_user BIGINT;
    DECLARE v_amount DECIMAL(18,2);

    SELECT user_id, amount 
    INTO v_user, v_amount
    FROM payouts
    WHERE id = p_payout_id;

    CALL sp_wallet_debit_secure(
        v_user,
        v_amount,
        'PAYOUT',
        p_payout_id,
        CONCAT('PAYOUT_', p_payout_id)
    );
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_wallet_pg_load` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_wallet_pg_load`(
    IN p_pg_txn_id BIGINT
)
BEGIN
    DECLARE v_user BIGINT;
    DECLARE v_amount DECIMAL(18,2);

    SELECT created_by_user, amount 
    INTO v_user, v_amount
    FROM pg_transactions
    WHERE id = p_pg_txn_id 
      AND status='SUCCESS';

    CALL sp_wallet_credit_secure(
        v_user,
        v_amount,
        'PG_TXN',
        p_pg_txn_id,
        CONCAT('PG_LOAD_', p_pg_txn_id)
    );
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2025-12-02  2:43:24
