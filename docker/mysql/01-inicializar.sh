#!/bin/bash
set -eu

ejecutar_sql() {
    MYSQL_PWD="${MYSQL_ROOT_PASSWORD}" mysql --protocol=socket --socket=/var/run/mysqld/mysqld.sock --user=root --comments "$@"
}

exigir_alfanumerica() {
    case "${2:-}" in
        ""|*[!A-Za-z0-9]*)
            echo "La variable $1 debe existir y contener solo letras y números." >&2
            exit 1
            ;;
    esac
}

exigir_hash_bcrypt() {
    if [[ ! "${2:-}" =~ ^\$2[aby]\$[0-9]{2}\$[./A-Za-z0-9]{53}$ ]]; then
        echo "La variable $1 debe contener un hash BCrypt válido." >&2
        exit 1
    fi
}

exigir_alfanumerica VENTAS_DB_PASSWORD "${VENTAS_DB_PASSWORD:-}"
exigir_alfanumerica USUARIOS_DB_PASSWORD "${USUARIOS_DB_PASSWORD:-}"
exigir_alfanumerica REPORTES_DB_PASSWORD "${REPORTES_DB_PASSWORD:-}"
exigir_hash_bcrypt ADMIN_PASSWORD_HASH "${ADMIN_PASSWORD_HASH:-}"
exigir_hash_bcrypt CAJERO_PASSWORD_HASH "${CAJERO_PASSWORD_HASH:-}"

esquema=/sql/bdMicroMercadoArqui.sql
if [ ! -r "${esquema}" ]; then
    echo "No se encuentra el esquema en ${esquema}." >&2
    exit 1
fi

echo "Creando el esquema y las semillas de la base de datos."
{
    printf "SET @ventas_demo_admin_password_hash='%s';\n" "${ADMIN_PASSWORD_HASH}"
    printf "SET @ventas_demo_cajero_password_hash='%s';\n" "${CAJERO_PASSWORD_HASH}"
    cat "${esquema}"
} | ejecutar_sql --default-character-set=utf8mb4 > /dev/null

echo "Creando las cuentas independientes de los servicios."
ejecutar_sql <<EOSQL
CREATE USER 'ventas_demo'@'%' IDENTIFIED BY '${VENTAS_DB_PASSWORD}' REQUIRE SSL;
GRANT ALL PRIVILEGES ON \`${MYSQL_DATABASE}\`.* TO 'ventas_demo'@'%';
CREATE USER 'usuarios_demo'@'%' IDENTIFIED BY '${USUARIOS_DB_PASSWORD}' REQUIRE SSL;
GRANT SELECT, INSERT, UPDATE ON \`${MYSQL_DATABASE}\`.\`USUARIO\` TO 'usuarios_demo'@'%';
GRANT SELECT ON \`${MYSQL_DATABASE}\`.\`ROL\` TO 'usuarios_demo'@'%';
CREATE USER 'reportes_demo'@'%' IDENTIFIED BY '${REPORTES_DB_PASSWORD}' REQUIRE SSL;
GRANT SELECT ON \`${MYSQL_DATABASE}\`.* TO 'reportes_demo'@'%';
FLUSH PRIVILEGES;
EOSQL

echo "Inicialización completada."
