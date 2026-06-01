CREATE TABLE IF NOT EXISTS public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'bom') THEN
            CREATE SCHEMA bom;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
    CREATE TABLE bom.audit_logs (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        entity_type character varying(50) NOT NULL,
        entity_id uuid NOT NULL,
        action character varying(20) NOT NULL,
        changed_by character varying(100) NOT NULL,
        changed_at timestamp with time zone NOT NULL DEFAULT (NOW()),
        old_values jsonb,
        new_values jsonb,
        CONSTRAINT "PK_audit_logs" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
    CREATE TABLE bom.boms (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        code character varying(50) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        item_code character varying(50) NOT NULL,
        item_name character varying(255) NOT NULL,
        yield_quantity numeric(18,6) NOT NULL,
        yield_unit character varying(50) NOT NULL,
        version integer NOT NULL DEFAULT 1,
        status character varying(20) NOT NULL DEFAULT 'Draft',
        created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
        updated_at timestamp with time zone NOT NULL DEFAULT (NOW()),
        created_by character varying(100) NOT NULL,
        CONSTRAINT "PK_boms" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
    CREATE TABLE bom.bom_assignments (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        item_code character varying(50) NOT NULL,
        item_name character varying(200) NOT NULL,
        bom_id uuid NOT NULL,
        assigned_at timestamp with time zone NOT NULL DEFAULT (NOW()),
        assigned_by character varying(100) NOT NULL,
        CONSTRAINT "PK_bom_assignments" PRIMARY KEY (id),
        CONSTRAINT "FK_bom_assignments_boms_bom_id" FOREIGN KEY (bom_id) REFERENCES bom.boms (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
    CREATE TABLE bom.bom_lines (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        bom_id uuid NOT NULL,
        material_code character varying(50) NOT NULL,
        material_name character varying(200) NOT NULL,
        quantity numeric(18,6) NOT NULL,
        unit character varying(20) NOT NULL,
        sub_bom_id uuid,
        sort_order integer NOT NULL DEFAULT 0,
        notes text,
        CONSTRAINT "PK_bom_lines" PRIMARY KEY (id),
        CONSTRAINT "FK_bom_lines_boms_bom_id" FOREIGN KEY (bom_id) REFERENCES bom.boms (id) ON DELETE CASCADE,
        CONSTRAINT "FK_bom_lines_boms_sub_bom_id" FOREIGN KEY (sub_bom_id) REFERENCES bom.boms (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
    CREATE TABLE bom.production_orders (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        order_no character varying(30) NOT NULL,
        bom_id uuid NOT NULL,
        bom_snapshot jsonb NOT NULL,
        item_code character varying(50) NOT NULL,
        item_name character varying(255) NOT NULL,
        quantity numeric(18,6) NOT NULL,
        status character varying(20) NOT NULL DEFAULT 'Pending',
        source_so_numbers text[] NOT NULL,
        source_doc_date_from date,
        source_doc_date_to date,
        created_by character varying(100) NOT NULL,
        created_via character varying(10) NOT NULL DEFAULT 'UI',
        created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
        notes text,
        CONSTRAINT "PK_production_orders" PRIMARY KEY (id),
        CONSTRAINT "FK_production_orders_boms_bom_id" FOREIGN KEY (bom_id) REFERENCES bom.boms (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
    CREATE TABLE bom.production_order_lines (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        production_order_id uuid NOT NULL,
        material_code character varying(50) NOT NULL,
        material_name character varying(200) NOT NULL,
        required_quantity numeric(18,6) NOT NULL,
        unit character varying(20) NOT NULL,
        CONSTRAINT "PK_production_order_lines" PRIMARY KEY (id),
        CONSTRAINT "FK_production_order_lines_production_orders_production_order_id" FOREIGN KEY (production_order_id) REFERENCES bom.production_orders (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
    CREATE INDEX idx_audit_changed_at ON bom.audit_logs (changed_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
    CREATE INDEX idx_audit_entity ON bom.audit_logs (entity_type, entity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
    CREATE INDEX idx_bom_assignments_bom_id ON bom.bom_assignments (bom_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
    CREATE UNIQUE INDEX idx_bom_assignments_item_code ON bom.bom_assignments (item_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
    CREATE INDEX idx_bom_lines_bom_id ON bom.bom_lines (bom_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
    CREATE INDEX "IX_bom_lines_sub_bom_id" ON bom.bom_lines (sub_bom_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
    CREATE UNIQUE INDEX idx_boms_code ON bom.boms (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
    CREATE INDEX idx_boms_status ON bom.boms (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
    CREATE INDEX idx_po_lines_production_order_id ON bom.production_order_lines (production_order_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
    CREATE INDEX idx_production_orders_created_at ON bom.production_orders (created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
    CREATE INDEX idx_production_orders_item_code ON bom.production_orders (item_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
    CREATE INDEX idx_production_orders_source_so ON bom.production_orders USING gin (source_so_numbers);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
    CREATE INDEX idx_production_orders_status ON bom.production_orders (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
    CREATE INDEX "IX_production_orders_bom_id" ON bom.production_orders (bom_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_production_orders_order_no" ON bom.production_orders (order_no);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260422090338_InitialCreate') THEN
    INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260422090338_InitialCreate', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424040515_CascadeDeleteBomAssignments') THEN
    ALTER TABLE bom.bom_assignments DROP CONSTRAINT "FK_bom_assignments_boms_bom_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424040515_CascadeDeleteBomAssignments') THEN
    ALTER TABLE bom.bom_assignments ADD CONSTRAINT "FK_bom_assignments_boms_bom_id" FOREIGN KEY (bom_id) REFERENCES bom.boms (id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424040515_CascadeDeleteBomAssignments') THEN
    INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260424040515_CascadeDeleteBomAssignments', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE bom.bom_assignments DROP CONSTRAINT "FK_bom_assignments_boms_bom_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE bom.bom_lines DROP CONSTRAINT "FK_bom_lines_boms_bom_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE bom.bom_lines DROP CONSTRAINT "FK_bom_lines_boms_sub_bom_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE bom.production_order_lines DROP CONSTRAINT "FK_production_order_lines_production_orders_production_order_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE bom.production_orders DROP CONSTRAINT "FK_production_orders_boms_bom_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE bom.production_orders DROP CONSTRAINT "PK_production_orders";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE bom.production_order_lines DROP CONSTRAINT "PK_production_order_lines";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE bom.boms DROP CONSTRAINT "PK_boms";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE bom.audit_logs DROP CONSTRAINT "PK_audit_logs";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE bom.bom_lines SET SCHEMA public;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE bom.bom_assignments SET SCHEMA public;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE bom.production_orders RENAME TO bom_production_orders;
    ALTER TABLE bom.bom_production_orders SET SCHEMA public;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE bom.production_order_lines RENAME TO bom_production_order_lines;
    ALTER TABLE bom.bom_production_order_lines SET SCHEMA public;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE bom.boms RENAME TO bom_boms;
    ALTER TABLE bom.bom_boms SET SCHEMA public;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE bom.audit_logs RENAME TO bom_audit_logs;
    ALTER TABLE bom.bom_audit_logs SET SCHEMA public;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER INDEX public."IX_production_orders_order_no" RENAME TO "IX_bom_production_orders_order_no";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER INDEX public."IX_production_orders_bom_id" RENAME TO "IX_bom_production_orders_bom_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE public.bom_production_orders ADD CONSTRAINT "PK_bom_production_orders" PRIMARY KEY (id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE public.bom_production_order_lines ADD CONSTRAINT "PK_bom_production_order_lines" PRIMARY KEY (id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE public.bom_boms ADD CONSTRAINT "PK_bom_boms" PRIMARY KEY (id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE public.bom_audit_logs ADD CONSTRAINT "PK_bom_audit_logs" PRIMARY KEY (id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE public.bom_assignments ADD CONSTRAINT "FK_bom_assignments_bom_boms_bom_id" FOREIGN KEY (bom_id) REFERENCES public.bom_boms (id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE public.bom_lines ADD CONSTRAINT "FK_bom_lines_bom_boms_bom_id" FOREIGN KEY (bom_id) REFERENCES public.bom_boms (id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE public.bom_lines ADD CONSTRAINT "FK_bom_lines_bom_boms_sub_bom_id" FOREIGN KEY (sub_bom_id) REFERENCES public.bom_boms (id) ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE public.bom_production_order_lines ADD CONSTRAINT "FK_bom_production_order_lines_bom_production_orders_production~" FOREIGN KEY (production_order_id) REFERENCES public.bom_production_orders (id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    ALTER TABLE public.bom_production_orders ADD CONSTRAINT "FK_bom_production_orders_bom_boms_bom_id" FOREIGN KEY (bom_id) REFERENCES public.bom_boms (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260424061924_MoveToPublicSchema') THEN
    INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260424061924_MoveToPublicSchema', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260523030943_AddBomProductionDocuments') THEN
    CREATE TABLE public.bom_production (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        doc_date date NOT NULL,
        doc_no character varying(30) NOT NULL,
        doc_time time NOT NULL,
        CONSTRAINT "PK_bom_production" PRIMARY KEY (id),
        CONSTRAINT "AK_bom_production_doc_no" UNIQUE (doc_no)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260523030943_AddBomProductionDocuments') THEN
    CREATE TABLE public.bom_production_detail (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        doc_no character varying(30) NOT NULL,
        item_code character varying(50) NOT NULL,
        qty numeric(18,6) NOT NULL,
        unit_code character varying(20) NOT NULL,
        CONSTRAINT "PK_bom_production_detail" PRIMARY KEY (id),
        CONSTRAINT "FK_bom_production_detail_bom_production_doc_no" FOREIGN KEY (doc_no) REFERENCES public.bom_production (doc_no) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260523030943_AddBomProductionDocuments') THEN
    CREATE INDEX idx_bom_production_doc_date ON public.bom_production (doc_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260523030943_AddBomProductionDocuments') THEN
    CREATE UNIQUE INDEX idx_bom_production_doc_no ON public.bom_production (doc_no);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260523030943_AddBomProductionDocuments') THEN
    CREATE INDEX idx_bom_production_detail_doc_no ON public.bom_production_detail (doc_no);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260523030943_AddBomProductionDocuments') THEN
    INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260523030943_AddBomProductionDocuments', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260525025231_RecreateBomProductionTables') THEN
    DROP TABLE public.bom_production_detail;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260525025231_RecreateBomProductionTables') THEN
    DROP TABLE public.bom_production_order_lines;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260525025231_RecreateBomProductionTables') THEN
    DROP TABLE public.bom_production;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260525025231_RecreateBomProductionTables') THEN
    DROP TABLE public.bom_production_orders;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260525025231_RecreateBomProductionTables') THEN
    CREATE TABLE public.bom_productions (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        doc_date date NOT NULL,
        doc_no character varying(30) NOT NULL,
        doc_time time NOT NULL,
        CONSTRAINT "PK_bom_productions" PRIMARY KEY (id),
        CONSTRAINT "AK_bom_productions_doc_no" UNIQUE (doc_no)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260525025231_RecreateBomProductionTables') THEN
    CREATE TABLE public.bom_production_orders (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        doc_no character varying(30) NOT NULL,
        doc_date date NOT NULL,
        ref_doc_no character varying(50) NOT NULL,
        ref_doc_date date NOT NULL,
        item_code character varying(50) NOT NULL,
        qty numeric(18,6) NOT NULL,
        unit_code character varying(50) NOT NULL,
        CONSTRAINT "PK_bom_production_orders" PRIMARY KEY (id),
        CONSTRAINT "FK_bom_production_orders_bom_productions_doc_no" FOREIGN KEY (doc_no) REFERENCES public.bom_productions (doc_no) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260525025231_RecreateBomProductionTables') THEN
    CREATE TABLE public.bom_production_details (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        doc_no character varying(30) NOT NULL,
        item_code character varying(50) NOT NULL,
        item_name character varying(255) NOT NULL,
        qty numeric(18,6) NOT NULL,
        unit_code character varying(50) NOT NULL,
        CONSTRAINT "PK_bom_production_details" PRIMARY KEY (id),
        CONSTRAINT "FK_bom_production_details_bom_productions_doc_no" FOREIGN KEY (doc_no) REFERENCES public.bom_productions (doc_no) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260525025231_RecreateBomProductionTables') THEN
    CREATE INDEX idx_bom_productions_doc_date ON public.bom_productions (doc_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260525025231_RecreateBomProductionTables') THEN
    CREATE UNIQUE INDEX idx_bom_productions_doc_no ON public.bom_productions (doc_no);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260525025231_RecreateBomProductionTables') THEN
    CREATE INDEX idx_bom_production_orders_doc_date ON public.bom_production_orders (doc_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260525025231_RecreateBomProductionTables') THEN
    CREATE INDEX idx_bom_production_orders_doc_no ON public.bom_production_orders (doc_no);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260525025231_RecreateBomProductionTables') THEN
    CREATE INDEX idx_bom_production_orders_item_code ON public.bom_production_orders (item_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260525025231_RecreateBomProductionTables') THEN
    CREATE INDEX idx_bom_production_orders_ref_doc_no ON public.bom_production_orders (ref_doc_no);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260525025231_RecreateBomProductionTables') THEN
    CREATE INDEX idx_bom_production_details_doc_no ON public.bom_production_details (doc_no);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260525025231_RecreateBomProductionTables') THEN
    CREATE INDEX idx_bom_production_details_item_code ON public.bom_production_details (item_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260525025231_RecreateBomProductionTables') THEN
    INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260525025231_RecreateBomProductionTables', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260528000000_AddWarehouseShelfToBomProductionDetails') THEN
    ALTER TABLE public.bom_production_details ADD wh_code character varying(50) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260528000000_AddWarehouseShelfToBomProductionDetails') THEN
    ALTER TABLE public.bom_production_details ADD shelf_code character varying(50) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260528000000_AddWarehouseShelfToBomProductionDetails') THEN
    INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260528000000_AddWarehouseShelfToBomProductionDetails', '10.0.7');
    END IF;
END $EF$;
COMMIT;

