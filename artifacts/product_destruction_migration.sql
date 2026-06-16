CREATE TABLE IF NOT EXISTS public.bom_product_destruction (
    doc_no varchar(50) PRIMARY KEY,
    doc_date date NOT NULL,
    wh_code varchar(50) NOT NULL,
    shelf_code varchar(50) NOT NULL,
    remark varchar(255) NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS public.bom_product_destruction_pictures (
    doc_no varchar(50) NOT NULL REFERENCES public.bom_product_destruction(doc_no) ON DELETE CASCADE,
    line_number smallint NOT NULL,
    image_guid varchar(50) NOT NULL,
    image_file bytea NOT NULL,
    CONSTRAINT pk_bom_product_destruction_pictures PRIMARY KEY (doc_no, line_number)
);

CREATE TABLE IF NOT EXISTS public.bom_product_destruction_detail (
    doc_no varchar(50) NOT NULL REFERENCES public.bom_product_destruction(doc_no) ON DELETE CASCADE,
    item_code varchar(50) NOT NULL,
    qty numeric NOT NULL,
    unit_code varchar(50) NOT NULL,
    wh_code varchar(50) NOT NULL,
    shelf_code varchar(50) NOT NULL,
    line_number integer NOT NULL,
    CONSTRAINT pk_bom_product_destruction_detail PRIMARY KEY (doc_no, line_number),
    CONSTRAINT ck_bom_product_destruction_detail_qty_positive CHECK (qty > 0)
);

CREATE INDEX IF NOT EXISTS idx_bom_product_destruction_doc_date
    ON public.bom_product_destruction(doc_date DESC);

CREATE INDEX IF NOT EXISTS idx_bom_product_destruction_pictures_doc_no
    ON public.bom_product_destruction_pictures(doc_no);

CREATE INDEX IF NOT EXISTS idx_bom_product_destruction_detail_doc_no
    ON public.bom_product_destruction_detail(doc_no);

CREATE INDEX IF NOT EXISTS idx_bom_product_destruction_detail_item_code
    ON public.bom_product_destruction_detail(item_code);
