using CMS.FeeService.DTOs;
using CMS.FeeService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CMS.FeeService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeeController : ControllerBase
    {
        private readonly IFeeService _service;

        public FeeController(IFeeService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var fees = await _service.GetAllAsync();
            return Ok(fees);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var fee = await _service.GetByIdAsync(id);
            if (fee == null) return NotFound(new { message = $"Fee with ID {id} not found" });
            return Ok(fee);
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetByStudent(int studentId)
        {
            var fees = await _service.GetByStudentAsync(studentId);
            return Ok(fees);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFeeDto dto)
        {
            var fee = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = fee.FeeId }, fee);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateFeeDto dto)
        {
            var fee = await _service.UpdateAsync(id, dto);
            if (fee == null) return NotFound(new { message = $"Fee with ID {id} not found" });
            return Ok(fee);
        }

        [HttpPost("{id}/pay")]
        public async Task<IActionResult> PayFee(int id, [FromBody] PayFeeDto dto)
        {
            var fee = await _service.PayFeeAsync(id, dto);
            if (fee == null) return NotFound(new { message = $"Fee with ID {id} not found" });
            return Ok(fee);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound(new { message = $"Fee with ID {id} not found" });
            return NoContent();
        }
    }
}
