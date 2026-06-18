-- Smart BOM deployment migration
-- Generated for deploying the current BOM schema into PostgreSQL public schema.
-- Safe to run more than once: it uses CREATE IF NOT EXISTS, ADD COLUMN IF NOT EXISTS,
-- constraint existence checks, and EF migration history upserts.

BEGIN;

CREATE TABLE IF NOT EXISTS public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

CREATE TABLE IF NOT EXISTS public.bom_boms (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    code character varying(50) NOT NULL,
    name character varying(200) NOT NULL,
    description text NULL,
    item_code character varying(50) NOT NULL,
    item_name character varying(255) NOT NULL,
    yield_quantity numeric(18,6) NOT NULL,
    yield_unit character varying(50) NOT NULL,
    version integer NOT NULL DEFAULT 1,
    status character varying(20) NOT NULL DEFAULT 'Draft',
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
    updated_at timestamp with time zone NOT NULL DEFAULT NOW(),
    created_by character varying(100) NOT NULL,
    CONSTRAINT "PK_bom_boms" PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.bom_lines (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    bom_id uuid NOT NULL,
    material_code character varying(50) NOT NULL,
    material_name character varying(200) NOT NULL,
    quantity numeric(18,6) NOT NULL,
    unit character varying(20) NOT NULL,
    sub_bom_id uuid NULL,
    sort_order integer NOT NULL DEFAULT 0,
    notes text NULL,
    CONSTRAINT "PK_bom_lines" PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.bom_assignments (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    item_code character varying(50) NOT NULL,
    item_name character varying(200) NOT NULL,
    bom_id uuid NOT NULL,
    assigned_at timestamp with time zone NOT NULL DEFAULT NOW(),
    assigned_by character varying(100) NOT NULL,
    CONSTRAINT "PK_bom_assignments" PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.bom_audit_logs (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    entity_type character varying(50) NOT NULL,
    entity_id uuid NOT NULL,
    action character varying(20) NOT NULL,
    changed_by character varying(100) NOT NULL,
    changed_at timestamp with time zone NOT NULL DEFAULT NOW(),
    old_values jsonb NULL,
    new_values jsonb NULL,
    CONSTRAINT "PK_bom_audit_logs" PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.bom_productions (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    doc_date date NOT NULL,
    doc_no character varying(30) NOT NULL,
    doc_time time without time zone NOT NULL,
    CONSTRAINT "PK_bom_productions" PRIMARY KEY (id),
    CONSTRAINT "AK_bom_productions_doc_no" UNIQUE (doc_no)
);

CREATE TABLE IF NOT EXISTS public.bom_production_orders (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    doc_no character varying(30) NOT NULL,
    doc_date date NOT NULL,
    ref_doc_no character varying(50) NOT NULL,
    ref_doc_date date NOT NULL,
    item_code character varying(50) NOT NULL,
    item_name character varying(255) NOT NULL DEFAULT '',
    qty numeric(18,6) NOT NULL,
    unit_code character varying(50) NOT NULL,
    CONSTRAINT "PK_bom_production_orders" PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.bom_production_details (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    doc_no character varying(30) NOT NULL,
    item_code character varying(50) NOT NULL,
    item_name character varying(255) NOT NULL,
    qty numeric(18,6) NOT NULL,
    unit_code character varying(50) NOT NULL,
    wh_code character varying(50) NOT NULL DEFAULT '',
    shelf_code character varying(50) NOT NULL DEFAULT '',
    CONSTRAINT "PK_bom_production_details" PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.bom_product_destruction (
    doc_no character varying(50) NOT NULL,
    doc_date date NOT NULL,
    wh_code character varying(50) NOT NULL,
    shelf_code character varying(50) NOT NULL,
    remark character varying(255) NOT NULL DEFAULT '',
    CONSTRAINT pk_bom_product_destruction PRIMARY KEY (doc_no)
);

CREATE TABLE IF NOT EXISTS public.bom_product_destruction_pictures (
    doc_no character varying(50) NOT NULL,
    line_number smallint NOT NULL,
    image_guid character varying(50) NOT NULL,
    image_file bytea NOT NULL,
    CONSTRAINT pk_bom_product_destruction_pictures PRIMARY KEY (doc_no, line_number)
);

CREATE TABLE IF NOT EXISTS public.bom_product_destruction_detail (
    doc_no character varying(50) NOT NULL,
    item_code character varying(50) NOT NULL,
    qty numeric NOT NULL,
    unit_code character varying(50) NOT NULL,
    wh_code character varying(50) NOT NULL,
    shelf_code character varying(50) NOT NULL,
    line_number integer NOT NULL,
    CONSTRAINT pk_bom_product_destruction_detail PRIMARY KEY (doc_no, line_number),
    CONSTRAINT ck_bom_product_destruction_detail_qty_positive CHECK (qty > 0)
);

CREATE TABLE IF NOT EXISTS public.bom_material_process (
    doc_no character varying(50) NOT NULL,
    doc_date character varying(10) NOT NULL,
    wh_code character varying(50) NOT NULL,
    shelf_code character varying(50) NOT NULL,
    remark character varying(255) NOT NULL DEFAULT '',
    total_cost numeric NOT NULL DEFAULT 0,
    CONSTRAINT pk_bom_material_process PRIMARY KEY (doc_no)
);

CREATE TABLE IF NOT EXISTS public.bom_material_process_use (
    doc_no character varying(50) NOT NULL,
    item_code character varying(50) NOT NULL,
    qty numeric NOT NULL,
    unit_code character varying(50) NOT NULL,
    wh_code character varying(50) NOT NULL,
    shelf_code character varying(50) NOT NULL,
    cost_per_unit numeric NOT NULL DEFAULT 0,
    total_cost numeric NOT NULL DEFAULT 0,
    line_number integer NOT NULL,
    CONSTRAINT pk_bom_material_process_use PRIMARY KEY (doc_no, line_number),
    CONSTRAINT ck_bom_material_process_use_qty_positive CHECK (qty > 0)
);

CREATE TABLE IF NOT EXISTS public.bom_material_process_finish_good (
    doc_no character varying(50) NOT NULL,
    item_code character varying(50) NOT NULL,
    qty numeric NOT NULL,
    unit_code character varying(50) NOT NULL,
    wh_code character varying(50) NOT NULL,
    shelf_code character varying(50) NOT NULL,
    cost_per_unit numeric NOT NULL DEFAULT 0,
    total_cost numeric NOT NULL DEFAULT 0,
    line_number integer NOT NULL,
    CONSTRAINT pk_bom_material_process_finish_good PRIMARY KEY (doc_no, line_number),
    CONSTRAINT ck_bom_material_process_finish_good_qty_positive CHECK (qty > 0)
);

ALTER TABLE IF EXISTS public.bom_production_orders
    ADD COLUMN IF NOT EXISTS item_name character varying(255) NOT NULL DEFAULT '';

ALTER TABLE IF EXISTS public.bom_production_details
    ADD COLUMN IF NOT EXISTS wh_code character varying(50) NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS shelf_code character varying(50) NOT NULL DEFAULT '';

ALTER TABLE IF EXISTS public.bom_material_process
    ADD COLUMN IF NOT EXISTS total_cost numeric NOT NULL DEFAULT 0;

ALTER TABLE IF EXISTS public.bom_material_process_use
    ADD COLUMN IF NOT EXISTS cost_per_unit numeric NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS total_cost numeric NOT NULL DEFAULT 0;

ALTER TABLE IF EXISTS public.bom_material_process_finish_good
    ADD COLUMN IF NOT EXISTS cost_per_unit numeric NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS total_cost numeric NOT NULL DEFAULT 0;

CREATE UNIQUE INDEX IF NOT EXISTS idx_bom_productions_doc_no ON public.bom_productions(doc_no);

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_bom_lines_bom_boms_bom_id') THEN
        ALTER TABLE public.bom_lines
            ADD CONSTRAINT "FK_bom_lines_bom_boms_bom_id"
            FOREIGN KEY (bom_id) REFERENCES public.bom_boms(id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_bom_lines_bom_boms_sub_bom_id') THEN
        ALTER TABLE public.bom_lines
            ADD CONSTRAINT "FK_bom_lines_bom_boms_sub_bom_id"
            FOREIGN KEY (sub_bom_id) REFERENCES public.bom_boms(id) ON DELETE SET NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_bom_assignments_bom_boms_bom_id') THEN
        ALTER TABLE public.bom_assignments
            ADD CONSTRAINT "FK_bom_assignments_bom_boms_bom_id"
            FOREIGN KEY (bom_id) REFERENCES public.bom_boms(id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_bom_production_orders_bom_productions_doc_no') THEN
        ALTER TABLE public.bom_production_orders
            ADD CONSTRAINT "FK_bom_production_orders_bom_productions_doc_no"
            FOREIGN KEY (doc_no) REFERENCES public.bom_productions(doc_no) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_bom_production_details_bom_productions_doc_no') THEN
        ALTER TABLE public.bom_production_details
            ADD CONSTRAINT "FK_bom_production_details_bom_productions_doc_no"
            FOREIGN KEY (doc_no) REFERENCES public.bom_productions(doc_no) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_bom_product_destruction_pictures_doc_no') THEN
        ALTER TABLE public.bom_product_destruction_pictures
            ADD CONSTRAINT fk_bom_product_destruction_pictures_doc_no
            FOREIGN KEY (doc_no) REFERENCES public.bom_product_destruction(doc_no) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_bom_product_destruction_detail_doc_no') THEN
        ALTER TABLE public.bom_product_destruction_detail
            ADD CONSTRAINT fk_bom_product_destruction_detail_doc_no
            FOREIGN KEY (doc_no) REFERENCES public.bom_product_destruction(doc_no) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_bom_material_process_use_doc_no') THEN
        ALTER TABLE public.bom_material_process_use
            ADD CONSTRAINT fk_bom_material_process_use_doc_no
            FOREIGN KEY (doc_no) REFERENCES public.bom_material_process(doc_no) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_bom_material_process_finish_good_doc_no') THEN
        ALTER TABLE public.bom_material_process_finish_good
            ADD CONSTRAINT fk_bom_material_process_finish_good_doc_no
            FOREIGN KEY (doc_no) REFERENCES public.bom_material_process(doc_no) ON DELETE CASCADE;
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS idx_boms_code ON public.bom_boms(code);
CREATE INDEX IF NOT EXISTS idx_boms_status ON public.bom_boms(status);
CREATE INDEX IF NOT EXISTS idx_bom_lines_bom_id ON public.bom_lines(bom_id);
CREATE INDEX IF NOT EXISTS "IX_bom_lines_sub_bom_id" ON public.bom_lines(sub_bom_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_bom_assignments_item_code ON public.bom_assignments(item_code);
CREATE INDEX IF NOT EXISTS idx_bom_assignments_bom_id ON public.bom_assignments(bom_id);
CREATE INDEX IF NOT EXISTS idx_audit_entity ON public.bom_audit_logs(entity_type, entity_id);
CREATE INDEX IF NOT EXISTS idx_audit_changed_at ON public.bom_audit_logs(changed_at DESC);

CREATE INDEX IF NOT EXISTS idx_bom_productions_doc_date ON public.bom_productions(doc_date);
CREATE INDEX IF NOT EXISTS idx_bom_production_orders_doc_no ON public.bom_production_orders(doc_no);
CREATE INDEX IF NOT EXISTS idx_bom_production_orders_doc_date ON public.bom_production_orders(doc_date);
CREATE INDEX IF NOT EXISTS idx_bom_production_orders_ref_doc_no ON public.bom_production_orders(ref_doc_no);
CREATE INDEX IF NOT EXISTS idx_bom_production_orders_item_code ON public.bom_production_orders(item_code);
CREATE INDEX IF NOT EXISTS idx_bom_production_details_doc_no ON public.bom_production_details(doc_no);
CREATE INDEX IF NOT EXISTS idx_bom_production_details_item_code ON public.bom_production_details(item_code);

CREATE INDEX IF NOT EXISTS idx_bom_product_destruction_doc_date ON public.bom_product_destruction(doc_date DESC);
CREATE INDEX IF NOT EXISTS idx_bom_product_destruction_pictures_doc_no ON public.bom_product_destruction_pictures(doc_no);
CREATE INDEX IF NOT EXISTS idx_bom_product_destruction_detail_doc_no ON public.bom_product_destruction_detail(doc_no);
CREATE INDEX IF NOT EXISTS idx_bom_product_destruction_detail_item_code ON public.bom_product_destruction_detail(item_code);

CREATE INDEX IF NOT EXISTS idx_bom_material_process_doc_date ON public.bom_material_process(doc_date DESC);
CREATE INDEX IF NOT EXISTS idx_bom_material_process_use_doc_no ON public.bom_material_process_use(doc_no);
CREATE INDEX IF NOT EXISTS idx_bom_material_process_use_item_code ON public.bom_material_process_use(item_code);
CREATE INDEX IF NOT EXISTS idx_bom_material_process_finish_good_doc_no ON public.bom_material_process_finish_good(doc_no);
CREATE INDEX IF NOT EXISTS idx_bom_material_process_finish_good_item_code ON public.bom_material_process_finish_good(item_code);

INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES
    ('20260422090338_InitialCreate', '10.0.7'),
    ('20260424040515_CascadeDeleteBomAssignments', '10.0.7'),
    ('20260424061924_MoveToPublicSchema', '10.0.7'),
    ('20260523030943_AddBomProductionDocuments', '10.0.7'),
    ('20260525025231_RecreateBomProductionTables', '10.0.7'),
    ('20260528000000_AddWarehouseShelfToBomProductionDetails', '10.0.7'),
    ('20260611000000_AddItemNameToBomProductionOrders', '10.0.7'),
    ('20260616000000_AddProductDestructionTables', '10.0.7'),
    ('20260617000000_AddProductManufacturingTables', '10.0.7'),
    ('20260618000000_AddProductManufacturingCosts', '10.0.7')
ON CONFLICT ("MigrationId") DO UPDATE
SET "ProductVersion" = EXCLUDED."ProductVersion";

COMMIT;
