-- db_full_reset_create_seed.sql
-- Purpose: Drop & recreate app + MassTransit tables, add indexes, seed sample data.
-- Target: PostgreSQL

BEGIN;
SET client_min_messages = WARNING;
SET search_path TO public;

-- Optional extensions
CREATE EXTENSION IF NOT EXISTS "pgcrypto";  -- gen_random_uuid()
-- CREATE EXTENSION IF NOT EXISTS "uuid-ossp"; -- alternative for uuid_generate_v4()

/* =========================================================
   1) DROP TABLES (MassTransit first, then app tables)
   ========================================================= */
-- MassTransit tables
DROP TABLE IF EXISTS "OutboxMessage" CASCADE;
DROP TABLE IF EXISTS "OutboxState" CASCADE;
DROP TABLE IF EXISTS "InboxState" CASCADE;

-- App tables (dependents first)
DROP TABLE IF EXISTS "KycCases" CASCADE;
DROP TABLE IF EXISTS "AuditRecords" CASCADE;
DROP TABLE IF EXISTS "Customers" CASCADE;

-- EF history (optional)
DROP TABLE IF EXISTS "__EFMigrationsHistory" CASCADE;

/* =========================================================
   2) CREATE APP TABLES
   ========================================================= */

-- -------------------------
-- Customers
-- -------------------------
CREATE TABLE "Customers" (
    "CustomerId"  uuid PRIMARY KEY,
    "FirstName"   varchar(100)                 NOT NULL,
    "LastName"    varchar(100)                 NOT NULL,
    "Dob"         date                         NOT NULL,
    "Email"       varchar(256)                 NOT NULL,
    "Phone"       varchar(32)                  NOT NULL,
    "Status"      integer                      NOT NULL, -- CustomerStatus: 0=PENDING_KYC,1=VERIFIED,2=CLOSED
    "CreatedAt"   timestamp without time zone  NOT NULL,
    "UpdatedAt"   timestamp without time zone  NULL
);

CREATE UNIQUE INDEX "IX_Customers_Email"
    ON "Customers" ("Email");

CREATE INDEX "IX_Customers_Phone"
    ON "Customers" ("Phone");

CREATE INDEX "IX_Customers_FirstName_LastName_Dob"
    ON "Customers" ("FirstName", "LastName", "Dob");

-- -------------------------
-- KycCases
-- -------------------------
CREATE TABLE "KycCases" (
    "KycCaseId"        uuid PRIMARY KEY,
    "CustomerId"       uuid                        NOT NULL,
    "Status"           integer                     NOT NULL, -- KycStatus: 0=PENDING,1=VERIFIED,2=FAILED
    "ProviderRef"      text                        NULL,
    "EvidenceRefsJson" jsonb                       NULL,
    "ConsentText"      text                        NOT NULL,
    "AcceptedAt"       timestamp without time zone NOT NULL,
    "CreatedAt"        timestamp without time zone NOT NULL,
    "CheckedAt"        timestamp without time zone NULL,

    CONSTRAINT "FK_KycCases_Customers_CustomerId"
        FOREIGN KEY ("CustomerId")
        REFERENCES "Customers" ("CustomerId")
        ON DELETE CASCADE
);

CREATE INDEX "IX_KycCases_CustomerId_Status"
    ON "KycCases" ("CustomerId", "Status");

-- -------------------------
-- AuditRecords
-- -------------------------
CREATE TABLE "AuditRecords" (
    "AuditRecordId"   uuid PRIMARY KEY,
    "EntityType"      text                        NOT NULL,  -- e.g., "Customer", "KycCase"
    "Action"          integer                     NOT NULL,  -- AuditAction: 0=CREATE,1=UPDATE
    "TargetEntityId"  uuid                        NULL,
    "RelatedEntityId" uuid                        NULL,
    "Actor"           varchar(256)                NOT NULL,
    "CorrelationId"   text                        NULL,
    "Timestamp"       timestamp with time zone    NOT NULL,  -- DateTimeOffset -> timestamptz
    "BeforeJson"      jsonb                       NULL,
    "AfterJson"       jsonb                       NULL,
    "Source"          text                        NULL,
    "Environment"     text                        NULL
);

CREATE INDEX "IX_AuditRecords_EntityType_TargetEntityId_Timestamp"
    ON "AuditRecords" ("EntityType", "TargetEntityId", "Timestamp");

CREATE INDEX "IX_AuditRecords_CorrelationId"
    ON "AuditRecords" ("CorrelationId");

CREATE INDEX "IX_AuditRecords_RelatedEntityId_Timestamp"
    ON "AuditRecords" ("RelatedEntityId", "Timestamp");

/* =========================================================
   3) CREATE MASSTRANSIT TABLES (Inbox/Outbox)
   Notes:
   - These match MassTransit.EntityFrameworkCore outbox/inbox defaults (v8+).
   - If you’ve customized schemas or use a different version, adjust accordingly.
   ========================================================= */

-- 3a) InboxState
-- One row per (InboxId, ConsumerId) pair. Tracks at-least-once delivery.
CREATE TABLE "InboxState" (
    "InboxId"        uuid                        NOT NULL,
    "ConsumerId"     uuid                        NOT NULL,
    "LockId"         uuid                        NOT NULL,
    "RowVersion"     bigint                      NOT NULL,
    "Received"       bigint                      NOT NULL DEFAULT 0, -- messages received count/sequence
    "Delivered"      bigint                      NOT NULL DEFAULT 0, -- messages delivered count/sequence
    "ExpirationTime" timestamp with time zone    NULL,

    CONSTRAINT "PK_InboxState" PRIMARY KEY ("InboxId", "ConsumerId")
);

CREATE INDEX "IX_InboxState_ExpirationTime"
    ON "InboxState" ("ExpirationTime");

-- 3b) OutboxState
-- One row per outbox instance (per service), used to coordinate outbox publishing.
CREATE TABLE "OutboxState" (
    "OutboxId"     uuid                        PRIMARY KEY,
    "LockId"       uuid                        NOT NULL,
    "RowVersion"   bigint                      NOT NULL,
    "Created"      timestamp with time zone    NOT NULL,
    "Delivered"    timestamp with time zone    NULL
);

-- 3c) OutboxMessage
-- Messages waiting to be dispatched by the outbox.
-- PK is composite (OutboxId, SequenceNumber) to support ordering and multi-instance.
CREATE TABLE "OutboxMessage" (
    "OutboxId"            uuid                        NOT NULL,
    "SequenceNumber"      bigint                      NOT NULL,
    "EnqueueTime"         timestamp with time zone    NULL,
    "SentTime"            timestamp with time zone    NULL,
    "Headers"             jsonb                       NULL,
    "Properties"          jsonb                       NULL,
    "Body"                bytea                       NOT NULL, -- serialized transport message
    "ContentType"         text                        NULL,
    "MessageId"           uuid                        NULL,
    "ConversationId"      uuid                        NULL,
    "CorrelationId"       uuid                        NULL,
    "InitiatorId"         uuid                        NULL,
    "RequestId"           uuid                        NULL,
    "SourceAddress"       text                        NULL,
    "DestinationAddress"  text                        NULL,
    "ResponseAddress"     text                        NULL,
    "FaultAddress"        text                        NULL,
    "ExpirationTime"      timestamp with time zone    NULL,

    CONSTRAINT "PK_OutboxMessage" PRIMARY KEY ("OutboxId", "SequenceNumber"),
    CONSTRAINT "FK_OutboxMessage_OutboxState_OutboxId"
        FOREIGN KEY ("OutboxId") REFERENCES "OutboxState" ("OutboxId") ON DELETE CASCADE
);

CREATE INDEX "IX_OutboxMessage_EnqueueTime"
    ON "OutboxMessage" ("EnqueueTime");

/* =========================================================
   4) SEED SAMPLE DATA
   ========================================================= */

-- Customers
INSERT INTO "Customers"
("CustomerId","FirstName","LastName","Dob","Email","Phone","Status","CreatedAt","UpdatedAt")
VALUES
  ('00112233-4455-6677-8899-aabbccddeeff','Rahul','Sharma','1994-04-11','rahul.sharma@example.com','+91-9876543210', 0, '2025-12-01 10:30:00', NULL),
  ('11112222-3333-4444-5555-666677778888','Neha','Kulkarni','1992-06-25','neha.kulkarni@example.com','+91-9822001122', 1, '2025-11-15 08:00:00', '2025-11-20 09:15:00'),
  ('99998888-7777-6666-5555-444433332222','Amit','Patil','1988-01-03','amit.patil@example.com','+91-9812345678', 2, '2025-10-10 11:45:00', '2025-12-05 14:00:00')
ON CONFLICT ("CustomerId") DO NOTHING;

-- KYC Cases
INSERT INTO "KycCases"
("KycCaseId","CustomerId","Status","ProviderRef","EvidenceRefsJson","ConsentText","AcceptedAt","CreatedAt","CheckedAt")
VALUES
  ('aaaaaaaa-bbbb-cccc-dddd-eeeeffff0001','00112233-4455-6677-8899-aabbccddeeff', 0, NULL,
   '["doc-POI-123","doc-POA-456"]'::jsonb,
   'I consent to eKYC verification for account onboarding.',
   '2025-12-01 10:35:00','2025-12-01 10:36:00', NULL),

  ('aaaaaaaa-bbbb-cccc-dddd-eeeeffff0002','11112222-3333-4444-5555-666677778888', 1, 'kyc-prov-789',
   '["doc-POI-777","doc-POA-888"]'::jsonb,
   'I consent to eKYC verification for account onboarding.',
   '2025-11-15 08:05:00','2025-11-15 08:06:00','2025-11-15 09:10:00')
ON CONFLICT ("KycCaseId") DO NOTHING;

-- AuditRecords (CREATE actions for Customers)
INSERT INTO "AuditRecords"
("AuditRecordId","EntityType","Action","TargetEntityId","RelatedEntityId",
 "Actor","CorrelationId","Timestamp","BeforeJson","AfterJson","Source","Environment")
VALUES
  (gen_random_uuid(),'Customer',0,'00112233-4455-6677-8899-aabbccddeeff',NULL,'CustomerService','corr-rahul-001','2025-12-01 10:36:30+00',NULL,
   '{"customerId":"00112233-4455-6677-8899-aabbccddeeff","firstName":"Rahul","lastName":"Sharma","email":"rahul.sharma@example.com","phone":"+91-9876543210","status":0}'::jsonb,
   'API','Development'),

  (gen_random_uuid(),'Customer',0,'11112222-3333-4444-5555-666677778888',NULL,'CustomerService','corr-neha-001','2025-11-15 08:06:05+00',NULL,
   '{"customerId":"11112222-3333-4444-5555-666677778888","firstName":"Neha","lastName":"Kulkarni","email":"neha.kulkarni@example.com","phone":"+91-9822001122","status":1}'::jsonb,
   'API','Development'),

  (gen_random_uuid(),'Customer',1,'99998888-7777-6666-5555-444433332222',NULL,'system:closure','corr-amit-001','2025-12-05 14:00:20+00',
   '{"customerId":"99998888-7777-6666-5555-444433332222","status":1}'::jsonb,
   '{"customerId":"99998888-7777-6666-5555-444433332222","status":2}'::jsonb,
   'API','Development');


   
ALTER TABLE "KycCases" 
RENAME COLUMN "EvidenceRefsJson" TO "EvidenceRefs";


COMMIT;