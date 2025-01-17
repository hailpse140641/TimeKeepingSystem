using DataAccess.InterfaceRepository;
using DataAccess.InterfaceService;
using Microsoft.AspNetCore.Mvc;

namespace TimeKeepingSystem.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class RiskPerformanceEmployeeController : ControllerBase
    {
        private readonly IRiskPerformanceEmployeeRepository _repo;

        public RiskPerformanceEmployeeController(IRiskPerformanceEmployeeRepository repository)
        {
            _repo = repository;
        }

        // GET: api/RiskPerformanceEmployee
        [HttpGet]
        public async Task<ActionResult<object>> GetAllRiskPerformanceEmployeeAsync(int month, int year)
        {
            return Ok(await _repo.GetEmployeesViolatingRiskSettingForMonth(month, year));
        }
    }
}
