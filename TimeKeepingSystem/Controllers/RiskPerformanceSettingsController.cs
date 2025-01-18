using BusinessObject.DTO;
using DataAccess.InterfaceRepository;
using DataAccess.InterfaceService;
using Microsoft.AspNetCore.Mvc;

namespace TimeKeepingSystem.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class RiskPerformanceSettingsController : ControllerBase
    {
        private readonly IRiskPerformanceSettingRepository _repo;

        public RiskPerformanceSettingsController(IRiskPerformanceSettingRepository repository)
        {
            _repo = repository;
        }

        // Get all RiskPerformanceSettings
        [HttpGet]
        public async Task<ActionResult<List<object>>> GetAllRiskPerformanceSettingsAsync()
        {
            var settings = await Task.Run(() => _repo.GetAllRiskPerformanceSettingsAsync());
            return Ok(settings);
        }

        // Get a single RiskPerformanceSetting by ID
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetRiskPerformanceSettingByIdAsync(Guid id)
        {
            var setting = await _repo.GetRiskPerformanceSettingByIdAsync(id);
            if (setting == null) return NotFound("Risk Performance Setting not found.");
            return Ok(setting);
        }

        // Create a new RiskPerformanceSetting
        [HttpPost]
        public async Task<ActionResult> CreateRiskPerformanceSettingAsync([FromBody] RiskPerformanceSettingDTO dto)
        {
            if (dto == null) return BadRequest("Invalid data.");
            var success = await _repo.CreateRiskPerformanceSettingAsync(dto.LateMinuteAllow, dto.EarlyLeaveMinuteAllow);
            if (success != null) return Ok(new { id = success, dto.LateMinuteAllow, dto.EarlyLeaveMinuteAllow});

            return BadRequest("Failed to create Risk Performance Setting.");
        }

        // Update an existing RiskPerformanceSetting
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateRiskPerformanceSettingAsync(Guid id, [FromBody] RiskPerformanceSettingDTO dto)
        {
            if (dto == null) return BadRequest("Invalid data.");

            var success = await _repo.UpdateRiskPerformanceSettingAsync(id, dto.LateMinuteAllow, dto.EarlyLeaveMinuteAllow);
            if (success) return Ok(new { status = "Success", id = id});

            return NotFound("Risk Performance Setting not found or update failed.");
        }

        // Soft Delete a RiskPerformanceSetting
        [HttpDelete("{id}")]
        public async Task<ActionResult> SoftDeleteRiskPerformanceSettingAsync(Guid id)
        {
            var success = await _repo.SoftDeleteRiskPerformanceSettingAsync(id);
            if (success) return NoContent();

            return NotFound("Risk Performance Setting not found or already deleted.");
        }
    }
}
