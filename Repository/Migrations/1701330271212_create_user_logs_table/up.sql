CREATE TABLE public.user_logs (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    user_id int8 NOT NULL,
    taken_at timestamptz NOT NULL DEFAULT now(),
    guild_id int8 NOT NULL,
    CONSTRAINT user_logs_pkey PRIMARY KEY (id)
);