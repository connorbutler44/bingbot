CREATE TABLE public.pokes (
    id uuid NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    sender_id int8 NOT NULL,
    recipient_id int8 NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE table public.user_settings (
    id int8 NOT NULL PRIMARY KEY,
    pokes_enabled bool NOT NULL DEFAULT true
);