USE [Labcore];
GO

IF OBJECT_ID('tempdb..#MensajesUnicos') IS NOT NULL 
    DROP TABLE #MensajesUnicos;

CREATE TABLE #MensajesUnicos (
    iqi_ana_id      INT,
    iqi_fechahora   DATETIME,
    iqi_num_msj     VARCHAR(100),
    iqi_tipo_msj    VARCHAR(20),
    iqi_msj         VARCHAR(MAX),
    iqi_ack_status  VARCHAR(20),
    iqi_orc4        VARCHAR(50)
);

INSERT INTO #MensajesUnicos
SELECT 
    iqi_ana_id, 
    iqi_fechahora, 
    iqi_num_msj, 
    iqi_tipo_msj, 
    iqi_msj, 
    iqi_ack_status,
    LTRIM(RTRIM(
        SUBSTRING(
            iqi_msj,
            CHARINDEX('|', iqi_msj, 
                CHARINDEX('|', iqi_msj, 
                    CHARINDEX('|', iqi_msj, 
                        CHARINDEX('ORC|', iqi_msj) + 4
                    ) + 1
                ) + 1
            ) + 1,
            CHARINDEX('|', iqi_msj, 
                CHARINDEX('|', iqi_msj, 
                    CHARINDEX('|', iqi_msj, 
                        CHARINDEX('|', iqi_msj, 
                            CHARINDEX('ORC|', iqi_msj) + 4
                        ) + 1
                    ) + 1
                ) + 1
            ) - 
            CHARINDEX('|', iqi_msj, 
                CHARINDEX('|', iqi_msj, 
                    CHARINDEX('|', iqi_msj, 
                        CHARINDEX('ORC|', iqi_msj) + 4
                    ) + 1
                ) + 1
            ) - 1
        )
    )) AS iqi_orc4
FROM (
    SELECT 
        iqi_ana_id, 
        iqi_fechahora, 
        iqi_num_msj, 
        iqi_tipo_msj, 
        iqi_msj, 
        iqi_ack_status,
        ROW_NUMBER() OVER(
            -- ✅ Partición correcta: por num_msj Y orc4 limpio
            PARTITION BY 
                iqi_num_msj,
                LTRIM(RTRIM(
                    SUBSTRING(
                        iqi_msj,
                        CHARINDEX('|', iqi_msj, 
                            CHARINDEX('|', iqi_msj, 
                                CHARINDEX('|', iqi_msj, 
                                    CHARINDEX('ORC|', iqi_msj) + 4
                                ) + 1
                            ) + 1
                        ) + 1,
                        CHARINDEX('|', iqi_msj, 
                            CHARINDEX('|', iqi_msj, 
                                CHARINDEX('|', iqi_msj, 
                                    CHARINDEX('|', iqi_msj, 
                                        CHARINDEX('ORC|', iqi_msj) + 4
                                    ) + 1
                                ) + 1
                            ) + 1
                        ) - 
                        CHARINDEX('|', iqi_msj, 
                            CHARINDEX('|', iqi_msj, 
                                CHARINDEX('|', iqi_msj, 
                                    CHARINDEX('ORC|', iqi_msj) + 4
                                ) + 1
                            ) + 1
                        ) - 1
                    )
                ))
            ORDER BY 
                CASE iqi_ack_status
                    WHEN 'AA' THEN 1
                    WHEN 'AE' THEN 2
                    ELSE CASE WHEN iqi_ack_status IS NOT NULL THEN 3 ELSE 4 END
                END,
                iqi_fechahora DESC
        ) AS FilaNumero
    FROM itqueue_in WITH (NOLOCK)
) AS SubConsulta
WHERE FilaNumero = 1;

-- 4. SINCRONIZAR ESTADOS
-- ✅ JOIN solo por num_msj + orc4, UPDATE solo si el estado cambió
UPDATE c
SET 
    c.iqc_estado_original = t.iqi_ack_status,
    c.iqc_sis_id          = t.iqi_orc4
FROM itqueue_inCheck c
INNER JOIN #MensajesUnicos t 
    ON  c.iqc_num_msj = t.iqi_num_msj 
    AND ISNULL(c.iqc_sis_id, '') = ISNULL(t.iqi_orc4, '')
WHERE 
    ISNULL(c.iqc_estado_original, '') <> ISNULL(t.iqi_ack_status, '');

-- 5. INSERTAR NUEVOS
INSERT INTO itqueue_inCheck (
    iqc_ana_id, 
    iqc_fechahora, 
    iqc_num_msj, 
    iqc_tipo_msj, 
    iqc_msj, 
    iqc_estado_original,
    iqc_sis_id,
    iqc_pro_checker,
    iqc_intentos
)
SELECT 
    t.iqi_ana_id, 
    t.iqi_fechahora, 
    t.iqi_num_msj, 
    t.iqi_tipo_msj, 
    t.iqi_msj, 
    t.iqi_ack_status,
    t.iqi_orc4,
    0,
    0
FROM #MensajesUnicos t
WHERE NOT EXISTS (
    SELECT 1 
    FROM itqueue_inCheck c WITH (NOLOCK) 
    WHERE c.iqc_num_msj = t.iqi_num_msj 
      AND ISNULL(c.iqc_sis_id, '') = ISNULL(t.iqi_orc4, '')
);

DROP TABLE #MensajesUnicos;