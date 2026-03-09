using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MvcCorePaginacionRegistros.Data;
using MvcCorePaginacionRegistros.Models;

#region VIEWS Y STORED PROCEDURES

/*
create view V_DEPARTAMENTOS_INDIVIDUAL
as
    select cast(
    ROW_NUMBER() over (order by DEPT_NO) as int)
    as POSICION
    , DEPT_NO, DNOMBRE, LOC from DEPT
go

create procedure SP_GRUPO_DEPARTAMENTOS
(@posicion int)
as
    select DEPT_NO, DNOMBRE, LOC from V_DEPARTAMENTOS_INDIVIDUAL
    where POSICION >= @posicion and POSICION < (@posicion + 2)
go
exec SP_GRUPO_DEPARTAMENTOS 1

create view V_GRUPO_EMPLEADOS
as
    select cast(row_number() over (order by APELLIDO) as int)
    as POSICION, EMP_NO, APELLIDO, OFICIO, SALARIO, DEPT_NO
    from EMP
go
create procedure SP_GRUPO_EMPLEADOS
(@posicion int)
as
    select EMP_NO, APELLIDO, OFICIO, SALARIO, DEPT_NO
    from V_GRUPO_EMPLEADOS
    where POSICION >= @posicion and POSICION < (@posicion + 3)
go

create procedure SP_GRUPO_EMPLEADOS_OFICIO(@posicion int, @oficio nvarchar(50), @registros int out)
--Almacenados el numero de registros filtrados
as
select @registros= COUNT(EMP_NO) from EMP
where OFICIO=@oficio
select EMP_NO, APELLIDO, OFICIO, SALARIO, DEPT_NO from (select cast(ROW_NUMBER()over (order by APELLIDO)as int) as
POSICION, EMP_NO, APELLIDO, OFICIO, SALARIO, DEPT_NO
FROM EMP
Where OFICIO=@oficio)QUERY
where (QUERY.POSICION>=@posicion AND QUERY.POSICION<(@posicion+3))
go

-- NUEVO STORED PROCEDURE
CREATE PROCEDURE SP_EMPLEADO_DEPT_POSICION
(
    @iddept INT,
    @posicion INT
)
AS
BEGIN
    SELECT EMP_NO, APELLIDO, OFICIO, SALARIO, DEPT_NO
    FROM (
        SELECT
            CAST(ROW_NUMBER() OVER (ORDER BY EMP_NO) AS INT) AS POSICION,
            EMP_NO,
            APELLIDO,
            OFICIO,
            SALARIO,
            DEPT_NO
        FROM EMP
        WHERE DEPT_NO = @iddept
    ) AS QUERY
    WHERE QUERY.POSICION = @posicion
END
GO
*/
#endregion

namespace MvcCorePaginacionRegistros.Repositories
{
    public class RepositoryHospital
    {
        private HospitalContext context;

        public RepositoryHospital(HospitalContext context)
        {
            this.context = context;
        }

        public async Task<int> GetNumeroRegistrosVistaDepartamentosAsync()
        {
            return await this.context.VistaDepartamentos.CountAsync();
        }

        public async Task<VistaDepartamento> GetVistaDepartamentoAsync(int posicion)
        {
            VistaDepartamento departamento = await this
                .context.VistaDepartamentos.Where(z => z.Posicion == posicion)
                .FirstOrDefaultAsync();
            return departamento;
        }

        public async Task<List<VistaDepartamento>> GetGrupoVistaDepartamentoAsync(int posicion)
        {
            //select * from V_DEPARTAMENTOS_INDIVIDUAL
            //where POSICION >= 1 and POSICION< (1 + 2)
            var consulta =
                from datos in this.context.VistaDepartamentos
                where datos.Posicion >= posicion && datos.Posicion < (posicion + 2)
                select datos;
            return await consulta.ToListAsync();
        }

        public async Task<List<Departamento>> GetGrupoDepartamentosAsync(int posicion)
        {
            string sql = "SP_GRUPO_DEPARTAMENTOS @posicion";
            SqlParameter pamPosicion = new SqlParameter("@posicion", posicion);
            var consulta = this.context.Departamentos.FromSqlRaw(sql, pamPosicion);
            return await consulta.ToListAsync();
        }

        public async Task<int> GetEmpleadosCountAsync()
        {
            return await this.context.Empleados.CountAsync();
        }

        public async Task<List<Empleado>> GetGrupoEmpleadosAsync(int posicion)
        {
            string sql = "SP_GRUPO_EMPLEADOS @posicion";
            SqlParameter pamPosicion = new SqlParameter("@posicion", posicion);
            var consulta = this.context.Empleados.FromSqlRaw(sql, pamPosicion);
            return await consulta.ToListAsync();
        }

        public async Task<int> GetEmpleadosOficioCountAsync(string oficio)
        {
            return await this.context.Empleados.Where(z => z.Oficio == oficio).CountAsync();
        }

        public async Task<List<Empleado>> GetGrupoEmpleadosOficioAsync(string oficio, int posicion)
        {
            string sql = "SP_GRUPO_EMPLEADOS_OFICIO @posicion, @oficio";
            SqlParameter pamPos = new SqlParameter("@posicion", posicion);
            SqlParameter pamOfi = new SqlParameter("@oficio", oficio);
            var consulta = this.context.Empleados.FromSqlRaw(sql, pamPos, pamOfi);
            return await consulta.ToListAsync();
        }

        public async Task<ModelEmpleadosOficio> GetGrupoEmpleadosOficioOutAsync(
            string oficio,
            int posicion
        )
        {
            string sql = "SP_GRUPO_EMPLEADOS_OFICIO @posicion, @oficio, @registros out";
            SqlParameter pamPos = new SqlParameter("@posicion", posicion);
            SqlParameter pamOfi = new SqlParameter("@oficio", oficio);
            SqlParameter pamReg = new SqlParameter("@registros", 0);
            pamReg.Direction = ParameterDirection.Output;
            pamReg.DbType = DbType.Int32;

            var consulta = this.context.Empleados.FromSqlRaw(sql, pamPos, pamOfi, pamReg);
            List<Empleado> empleados = await consulta.ToListAsync();
            //HASTA QUE NO HEMOS EXTRAIDO LOS DATOS (Empleados)
            //NO SE LIBERAN LOS PARAMETROS DE SALIDA
            int registros = (int)pamReg.Value;
            return new ModelEmpleadosOficio { Empleados = empleados, NumeroRegistros = registros };
        }

        // MÉTODOS NUEVOS CON NOMBRES UNIFICADOS
        public async Task<List<Departamento>> GetDepartamentosAsync()
        {
            return await this.context.Departamentos.ToListAsync();
        }

        public async Task<Departamento> FindDepartamentoAsync(int id)
        {
            return await this
                .context.Departamentos.Where(z => z.IdDepartamento == id)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Empleado>> GetEmpleadosDeptAsync(int iddept)
        {
            return await this
                .context.Empleados.Where(z => z.IdDepartamento == iddept)
                .OrderBy(e => e.IdEmpleado)
                .ToListAsync();
        }

        public async Task<int> GetNumeroEmpleadosDeptAsync(int iddept)
        {
            return await this.context.Empleados.Where(z => z.IdDepartamento == iddept).CountAsync();
        }

        public async Task<Empleado> GetEmpleadoDeptPosicionAsync(int iddept, int posicion)
        {
            string sql = "SP_EMPLEADO_DEPT_POSICION @iddept, @posicion";
            SqlParameter pamDept = new SqlParameter("@iddept", iddept);
            SqlParameter pamPos = new SqlParameter("@posicion", posicion);

            var consulta = this.context.Empleados.FromSqlRaw(sql, pamDept, pamPos);
            return await consulta.FirstOrDefaultAsync();
        }
    }
}
