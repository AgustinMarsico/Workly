-- =========================================================
-- DATOS DE PRUEBA RF8 - 3000 rooms + ~29673 reservations
-- Empresa: db8c4fd0-3bd3-4244-adca-a18376ec032e
-- Usuario: 375a6191-312e-43f1-b254-848484944107
-- =========================================================

SET SESSION cte_max_recursion_depth = 5000;

START TRANSACTION;

-- ---------------------------------------------------------
-- 1) Limpiar datos previos de esta empresa
-- ---------------------------------------------------------

DELETE r
FROM reservations r
JOIN rooms rm ON rm.Id = r.RoomId
WHERE rm.CompanyId = 'db8c4fd0-3bd3-4244-adca-a18376ec032e';

DELETE FROM rooms
WHERE CompanyId = 'db8c4fd0-3bd3-4244-adca-a18376ec032e';

-- ---------------------------------------------------------
-- 2) Insertar 3000 rooms
-- ---------------------------------------------------------

INSERT INTO rooms (`Id`, `Name`, `MaxCapacity`, `Equipment`, `type_key`, `CompanyId`)
WITH RECURSIVE seq AS (
    SELECT 1 AS n
    UNION ALL
    SELECT n + 1
    FROM seq
    WHERE n < 3000
)
SELECT
    LOWER(CONCAT(
        LPAD(HEX(n), 8, '0'), '-',
        LPAD(HEX(n), 4, '0'), '-',
        '4444-',
        '8888-',
        LPAD(HEX(n), 12, '0')
    )) AS Id,
    CONCAT('Room ', LPAD(n, 4, '0')) AS Name,
    2 + (n % 20) AS MaxCapacity,
    CONCAT('Equipment set ', n) AS Equipment,
    CASE
        WHEN n % 3 = 0 THEN 'meeting_room'
        WHEN n % 3 = 1 THEN 'desk'
        ELSE 'auditorium'
    END AS type_key,
    'db8c4fd0-3bd3-4244-adca-a18376ec032e' AS CompanyId
FROM seq;

-- ---------------------------------------------------------
-- 3) Room 0001 -> 1200 reservas activas
-- ---------------------------------------------------------

INSERT INTO reservations (`Id`, `RoomId`, `UserId`, `StartUtc`, `EndUtc`, `CancelledAtUtc`, `CancelledByUserId`)
WITH RECURSIVE seq AS (
    SELECT 1 AS n
    UNION ALL
    SELECT n + 1
    FROM seq
    WHERE n < 1200
)
SELECT
    LOWER(CONCAT(
        LPAD(HEX(n), 8, '0'),
        '-1111-1111-1111-',
        LPAD(HEX(n), 12, '0')
    )) AS Id,
    '00000001-0001-4444-8888-000000000001' AS RoomId,
    '375a6191-312e-43f1-b254-848484944107' AS UserId,
    TIMESTAMP('2026-01-01 08:00:00') + INTERVAL n HOUR AS StartUtc,
    TIMESTAMP('2026-01-01 09:00:00') + INTERVAL n HOUR AS EndUtc,
    NULL AS CancelledAtUtc,
    NULL AS CancelledByUserId
FROM seq;

-- ---------------------------------------------------------
-- 4) Room 0002 -> 900 reservas activas
-- ---------------------------------------------------------

INSERT INTO reservations (`Id`, `RoomId`, `UserId`, `StartUtc`, `EndUtc`, `CancelledAtUtc`, `CancelledByUserId`)
WITH RECURSIVE seq AS (
    SELECT 1 AS n
    UNION ALL
    SELECT n + 1
    FROM seq
    WHERE n < 900
)
SELECT
    LOWER(CONCAT(
        LPAD(HEX(n + 500000), 8, '0'),
        '-2222-2222-2222-',
        LPAD(HEX(n + 500000), 12, '0')
    )) AS Id,
    '00000002-0002-4444-8888-000000000002' AS RoomId,
    '375a6191-312e-43f1-b254-848484944107' AS UserId,
    TIMESTAMP('2026-02-01 08:00:00') + INTERVAL n HOUR AS StartUtc,
    TIMESTAMP('2026-02-01 09:00:00') + INTERVAL n HOUR AS EndUtc,
    NULL AS CancelledAtUtc,
    NULL AS CancelledByUserId
FROM seq;

-- ---------------------------------------------------------
-- 5) Room 0003 -> 600 reservas activas
-- ---------------------------------------------------------

INSERT INTO reservations (`Id`, `RoomId`, `UserId`, `StartUtc`, `EndUtc`, `CancelledAtUtc`, `CancelledByUserId`)
WITH RECURSIVE seq AS (
    SELECT 1 AS n
    UNION ALL
    SELECT n + 1
    FROM seq
    WHERE n < 600
)
SELECT
    LOWER(CONCAT(
        LPAD(HEX(n + 900000), 8, '0'),
        '-3333-3333-3333-',
        LPAD(HEX(n + 900000), 12, '0')
    )) AS Id,
    '00000003-0003-4444-8888-000000000003' AS RoomId,
    '375a6191-312e-43f1-b254-848484944107' AS UserId,
    TIMESTAMP('2026-03-01 08:00:00') + INTERVAL n HOUR AS StartUtc,
    TIMESTAMP('2026-03-01 09:00:00') + INTERVAL n HOUR AS EndUtc,
    NULL AS CancelledAtUtc,
    NULL AS CancelledByUserId
FROM seq;

-- ---------------------------------------------------------
-- 6) Resto de rooms (0004 a 3000)
--    9 reservas por room:
--      - 7 activas
--      - 2 canceladas
-- ---------------------------------------------------------

INSERT INTO reservations (`Id`, `RoomId`, `UserId`, `StartUtc`, `EndUtc`, `CancelledAtUtc`, `CancelledByUserId`)
WITH RECURSIVE room_seq AS (
    SELECT 4 AS room_n
    UNION ALL
    SELECT room_n + 1
    FROM room_seq
    WHERE room_n < 3000
),
res_seq AS (
    SELECT 1 AS res_n
    UNION ALL
    SELECT res_n + 1
    FROM res_seq
    WHERE res_n < 9
)
SELECT
    LOWER(CONCAT(
        LPAD(HEX(room_n), 4, '0'),
        LPAD(HEX(res_n), 4, '0'),
        '-5555-5555-5555-',
        LPAD(HEX(room_n * 100 + res_n), 12, '0')
    )) AS Id,
    LOWER(CONCAT(
        LPAD(HEX(room_n), 8, '0'), '-',
        LPAD(HEX(room_n), 4, '0'), '-',
        '4444-',
        '8888-',
        LPAD(HEX(room_n), 12, '0')
    )) AS RoomId,
    '375a6191-312e-43f1-b254-848484944107' AS UserId,
    TIMESTAMP('2026-04-01 08:00:00') + INTERVAL (room_n * 2 + res_n) HOUR AS StartUtc,
    TIMESTAMP('2026-04-01 09:00:00') + INTERVAL (room_n * 2 + res_n) HOUR AS EndUtc,
    CASE
        WHEN res_n IN (8, 9)
            THEN TIMESTAMP('2026-04-01 07:00:00') + INTERVAL (room_n * 2 + res_n) HOUR
        ELSE NULL
    END AS CancelledAtUtc,
    CASE
        WHEN res_n IN (8, 9)
            THEN '375a6191-312e-43f1-b254-848484944107'
        ELSE NULL
    END AS CancelledByUserId
FROM room_seq
CROSS JOIN res_seq;

COMMIT;

-- ---------------------------------------------------------
-- 7) Validaciones
-- ---------------------------------------------------------

-- Cantidad de rooms
SELECT COUNT(*) AS RoomsCount
FROM rooms
WHERE CompanyId = 'db8c4fd0-3bd3-4244-adca-a18376ec032e';

-- Cantidad total de reservations para esta empresa
SELECT COUNT(*) AS ReservationsCount
FROM reservations r
JOIN rooms rm ON rm.Id = r.RoomId
WHERE rm.CompanyId = 'db8c4fd0-3bd3-4244-adca-a18376ec032e';

-- Top 3 esperado para RF8
SELECT
    r.RoomId,
    rm.Name,
    COUNT(*) AS ReservationsCount
FROM reservations r
JOIN rooms rm ON rm.Id = r.RoomId
WHERE rm.CompanyId = 'db8c4fd0-3bd3-4244-adca-a18376ec032e'
  AND r.CancelledAtUtc IS NULL
GROUP BY r.RoomId, rm.Name
ORDER BY ReservationsCount DESC, rm.Name ASC
LIMIT 3;