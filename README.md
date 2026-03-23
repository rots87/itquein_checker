# Itquein Checker Engine (Fase 2) 🚀

![Status](https://img.shields.io/badge/Status-Producción--Estable-success)
![Platform](https://img.shields.io/badge/Platform-.NET%209.0-blue)
![License](https://img.shields.io/badge/License-Apache%202.0-orange)

**Itquein Checker** es un middleware de resiliencia de datos diseñado para el entorno hospitalario. Su función principal es garantizar la integridad y el flujo de órdenes clínicas entre el sistema **SIAP** y el LIS **Labcore**, automatizando la recuperación de errores de transmisión HL7.

---

## 📋 El Problema (Contexto Crítico)
En entornos de alta carga, el middleware estándar puede presentar inconsistencias debido a:
- **Colisión de IDs:** Órdenes "gemelas" enviadas al mismo segundo que comparten ID de control pero pertenecen a diferentes pacientes.
- **Registros Huérfanos:** Errores de aplicación (AE) que bloquean la reentrada de datos en el sistema de laboratorio.
- **Fallas de Red:** Timeouts de comunicación que dejan las órdenes en un limbo operativo sin registro de éxito.

## ✨ Solución y Características (Fase 2)
Este motor implementa una lógica de **Self-Healing** (Autocuración) avanzada:

- **Validación de Doble Llave:** Implementación de búsqueda por `MSH-10` (Control ID) y `ORC-4` (SIS ID) para diferenciar inequívocamente cada orden, eliminando el riesgo de sobreescritura.
- **Protocolo de Rescate (Minuto 6):** Lógica de maduración que espera un intervalo de seguridad antes de limpiar "cadáveres" en el Restlink y forzar una reinyección limpia.
- **Control de Ciclo de Vida:** Límite estricto de 5 intentos con marcado automático de "Fallo Fatal", notificando visualmente y mediante logs para intervención manual.
- **Logging de Auditoría:** Registro persistente mediante un sistema de archivos para trazabilidad técnica y forense de cada mensaje procesado.

---

## 🛠️ Stack Tecnológico
- **Lenguaje:** C# (.NET 9.0 Windows Form)
- **Base de Datos:** SQL Server (T-SQL, Jobs, Transacciones)
- **Mensajería:** HL7 (Health Level Seven Standard)
- **Arquitectura:** Programación Asíncrona y Patrones de Resiliencia.

---

## ⚙️ Arquitectura del Motor
El motor opera en ciclos de 30 segundos siguiendo este flujo lógico:

1. **Lectura:** Obtiene órdenes pendientes de la tabla de tránsito (itqueue_inCheck).
2. **Validación Dual:** Verifica contra la tabla maestra de Labcore usando la identidad combinada (NumMsj + SisId).
3. **Decisión Automática:** - **Éxito:** Si existe en destino, marca como procesada (Verde).
   - **Maduración:** Si no existe y tiene < 6 min, permanece en espera (Amarillo).
   - **Rescate:** Si no existe y tiene > 6 min, ejecuta limpieza de tablas e inyección vía POST al puerto 81 (Azul).
4. **Finalización:** Registro de resultados en base de datos y archivo de log físico.

---

## 🚀 Instalación y Uso

1. **Clonar el repositorio:**
   ```bash
   git clone [https://github.com/rots87/itquein_checker.git](https://github.com/rots87/itquein_checker.git)
