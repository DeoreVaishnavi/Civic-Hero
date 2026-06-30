
using CivicHero.Backend.Core.DTOs.Wards;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WardsController : ControllerBase
    {
        private readonly WardRepository _wardRepository;

        public WardsController(WardRepository wardRepository)
        {
            _wardRepository = wardRepository;
        }
        [HttpGet]
        public async Task<IActionResult> getWard()
        {
            var wards = await _wardRepository.GetAllAsync();

            var wardDtos = wards.Select(ward => new WardDto
            {
                Id = ward.Id,
                Name = ward.Name,
                Code = ward.Code,
                BoundaryGeoJson = ward.BoundaryGeoJson,
                CreatedAt = ward.CreatedAt
            }).ToList();

            return Ok(wardDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetWardById(int id)
        {
            var ward = await _wardRepository.GetByIdAsync(id);

            if (ward == null)
            {
                return NotFound();
            }

            var wardDto = new WardDto
            {
                Id = ward.Id,
                Name = ward.Name,
                Code = ward.Code,
                BoundaryGeoJson = ward.BoundaryGeoJson,
                CreatedAt = ward.CreatedAt
            };

            return Ok(wardDto);
        }

        [HttpPost]
        public async Task<IActionResult> creatWard(CreateWardDto createwardDto)
        {
            var wardExist = await _wardRepository.GetByCodeAsync(createwardDto.Code);

            if (wardExist != null)
                return BadRequest("The Ward With This Code Already Exist.");

            var ward = new Ward
            {
                Name = createwardDto.Name,
                Code = createwardDto.Code,
                BoundaryGeoJson = createwardDto.BoundaryGeoJson,
                CreatedAt = DateTime.UtcNow
            };

            var createdWard = await _wardRepository.AddAsync(ward);

            var wardDto = new WardDto
            {
                Id = createdWard.Id,
                Name = createdWard.Name,
                Code = createdWard.Code,
                BoundaryGeoJson = createdWard.BoundaryGeoJson,
                CreatedAt = createdWard.CreatedAt
            };
            return CreatedAtAction(nameof(GetWardById), new { id = wardDto.Id }, wardDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWard(int id, UpdateWardDto updateWardDto)
        {
            var ward = await _wardRepository.GetByIdAsync(id);

            if (ward == null)
            {
                return NotFound();
            }

            var existingWardWithCode = await _wardRepository.GetByCodeAsync(updateWardDto.Code);

            if (existingWardWithCode != null && existingWardWithCode.Id != id)
            {
                return BadRequest("Another ward with this code already exists.");
            }

            ward.Name = updateWardDto.Name;
            ward.Code = updateWardDto.Code;
            ward.BoundaryGeoJson = updateWardDto.BoundaryGeoJson;

            var updatedWard = await _wardRepository.UpdateAsync(ward);

            var wardDto = new WardDto
            {
                Id = updatedWard.Id,
                Name = updatedWard.Name,
                Code = updatedWard.Code,
                BoundaryGeoJson = updatedWard.BoundaryGeoJson,
                CreatedAt = updatedWard.CreatedAt
            };

            return Ok(wardDto);
        }
    }
}
