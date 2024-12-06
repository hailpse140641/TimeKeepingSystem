
using BusinessObject.DTO;
using DataAccess.InterfaceRepository;
using DataAccess.InterfaceService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TimeKeepingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RequestController : ControllerBase
    {
        private readonly IAttendanceStatusService _service;
        private readonly IRequestLeaveService _requestLeaveService;
        private readonly IRequestRepository _requestRepository;

        public RequestController(IAttendanceStatusService service, IRequestRepository requestRepository)
        {
            _service = service;
            _requestRepository = requestRepository;
        }

        [HttpGet("get-all-request-type-of-employee")]
        public async Task<ActionResult<CombinedRequestDTO>> GetAllRequestTypeOfEmployeeById(Guid employeeId, string? dateFilter)
        {
            try
            {
                return Ok(await _requestRepository.GetAllRequestTypesOfEmployeeById(employeeId, dateFilter));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("get-all-request-id-not-not-have-employee")]
        public async Task<ActionResult<List<Guid>>> FindRequestsWithMissingEmployees()
        {
            try
            {
                return Ok(await _requestRepository.FindRequestsWithMissingEmployees());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("clean-invalid-request")]
        public async Task<ActionResult<int>> SoftDeleteInvalidRequests()
        {
            try
            {
                return Ok(await _requestRepository.SoftDeleteInvalidRequests());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

