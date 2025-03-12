CREATE TABLE public.user_logs (
    id uuid NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    user_id int8 NOT NULL,
    taken_at timestamptz NOT NULL DEFAULT now(),
    guild_id int8 NOT NULL
);