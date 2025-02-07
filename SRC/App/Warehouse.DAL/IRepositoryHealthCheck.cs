/********************************************************************************
* IRepositoryHealthCheck.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Warehouse.DAL.Registrations
{
    public interface IRepositoryHealthCheck : IHealthCheck { }
}
