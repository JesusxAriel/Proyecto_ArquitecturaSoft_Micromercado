-- Migración 001: el código de categoría debe ser único.
-- Aplicar sobre una BD existente (bdMicroMercadoArqui). No borra datos.
USE `bdMicroMercadoArqui`;

-- 1) Verificar duplicados. Si devuelve filas, corregir esos códigos ANTES de continuar
--    (el ALTER fallaría con error 1062).
SELECT `codigo`, COUNT(*) AS repeticiones
FROM `CATEGORIAS`
GROUP BY `codigo`
HAVING COUNT(*) > 1;

-- 2) Crear el índice UNIQUE.
ALTER TABLE `CATEGORIAS`
  ADD CONSTRAINT `UQ_Categorias_codigo` UNIQUE (`codigo`);
