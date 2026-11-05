-- PLANTILLA MySQL 8.4.11. NO EJECUTADA. Usar Activar-CuentaDemo.ps1 tras autorización.
-- El cliente sustituye __PM_PASSWORD_SQL_LITERAL__ SOLO EN MEMORIA.
-- No guardar la versión sustituida, no usar --force ni administración concurrente.
-- Host observado desde Windows el 2026-11-05: 172.20.0.1; volver a comprobar antes.
-- El secreto aparece únicamente en CREATE USER, sujeto a reescritura de logs MySQL.
-- Nunca SET @password, CALL con contraseña ni SET @sql con contraseña.
SET @pm_u_existe=(SELECT COUNT(*) FROM mysql.user WHERE User='usuarios_demo');
-- Si existe bajo OTRO host, no ejecutar este archivo: el cliente debe detenerse.
-- IF NOT EXISTS no modifica contraseña, TLS ni permisos de una cuenta existente.
CREATE USER IF NOT EXISTS 'usuarios_demo'@'172.20.0.1'
  IDENTIFIED WITH caching_sha2_password BY __PM_PASSWORD_SQL_LITERAL__ REQUIRE SSL;
-- Con cuenta preexistente no se ejecuta ningún GRANT.
SET @pm_u_sql=IF(@pm_u_existe=0,
 'GRANT SELECT, INSERT, UPDATE ON `puntomarket_ventas_demo_local`.`USUARIO` TO ''usuarios_demo''@''172.20.0.1''',
 'SELECT 0 AS permisos_USUARIO_modificados');
PREPARE pm_u_admin FROM @pm_u_sql;
EXECUTE pm_u_admin;
DEALLOCATE PREPARE pm_u_admin;
SET @pm_u_sql=IF(@pm_u_existe=0,
 'GRANT SELECT ON `puntomarket_ventas_demo_local`.`ROL` TO ''usuarios_demo''@''172.20.0.1''',
 'SELECT 0 AS permisos_ROL_modificados');
PREPARE pm_u_admin FROM @pm_u_sql;
EXECUTE pm_u_admin;
DEALLOCATE PREPARE pm_u_admin;
SET @pm_u_sql=NULL;
SHOW GRANTS FOR 'usuarios_demo'@'172.20.0.1';
-- REQUIRE SSL; USUARIO SELECT/INSERT/UPDATE; ROL SELECT; sin roles/globales/GRANT OPTION.
-- OMITIR íntegramente si existe usuarios_demo: revisar por separado, no reparar.
-- CREATE/GRANT no son una transacción reversible. No ejecutar esta plantilla sin el cliente:
-- el marcador NO es una contraseña apta y jamás debe llegar al servidor.
