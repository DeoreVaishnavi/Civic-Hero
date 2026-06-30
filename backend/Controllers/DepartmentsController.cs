using CivicHero.Backend.Core.DTOs.Departments;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentsController : ControllerBase
    {
        private readonly DepartmentRepository _departmentRepository;

        public DepartmentsController(DepartmentRepository departmentRepository)
        {
            _departmentRepository = departmentRepository;
        }
        [HttpGet]
        public async Task<IActionResult> getDepartment()
        {
            var departments = await _departmentRepository.GetAllAsync();
            var departmentsDtos = departments.Select(department => new DepartmentDto
            {
                Id = department.Id,
                Name = department.Name,
                Description = department.Description,
            }).ToList();
            return Ok(departmentsDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDepartmentById(int id)
        {
            var department = await _departmentRepository.GetByIdAsync(id);

            if (department == null)
            {
                return NotFound();
            }

            var departmentDto = new DepartmentDto
            {
                Id = department.Id,
                Name = department.Name,
                Description = department.Description,
                CreatedAt = department.CreatedAt
            };

            return Ok(departmentDto);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDepartment(CreateDepartmentDto createDepartmentDto)
        {
            var existingDepartment = await _departmentRepository.GetByNameAsync(createDepartmentDto.Name);

            if (existingDepartment != null)
            {
                return BadRequest("Department with this name already exists.");
            }

            var department = new Department
            {
                Name = createDepartmentDto.Name,
                Description = createDepartmentDto.Description,
                CreatedAt = DateTime.UtcNow
            };

            var createdDepartment = await _departmentRepository.AddAsync(department);

            var departmentDto = new DepartmentDto
            {
                Id = createdDepartment.Id,
                Name = createdDepartment.Name,
                Description = createdDepartment.Description,
                CreatedAt = createdDepartment.CreatedAt
            };

            return CreatedAtAction(
                nameof(GetDepartmentById),
                new { id = departmentDto.Id },
                departmentDto
            );
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDepartment(int id, UpdateDepartmentDto updateDepartmentDto)
        {
            var department = await _departmentRepository.GetByIdAsync(id);

            if (department == null)
            {
                return NotFound();
            }

            var existingDepartmentWithName = await _departmentRepository.GetByNameAsync(updateDepartmentDto.Name);

            if (existingDepartmentWithName != null && existingDepartmentWithName.Id != id)
            {
                return BadRequest("Another department with this name already exists.");
            }

            department.Name = updateDepartmentDto.Name;
            department.Description = updateDepartmentDto.Description;

            var updatedDepartment = await _departmentRepository.UpdateAsync(department);

            var departmentDto = new DepartmentDto
            {
                Id = updatedDepartment.Id,
                Name = updatedDepartment.Name,
                Description = updatedDepartment.Description,
                CreatedAt = updatedDepartment.CreatedAt
            };

            return Ok(departmentDto);
        }
    }
}
