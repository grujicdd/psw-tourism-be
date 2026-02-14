// src/Modules/Tours/Explorer.Tours.Core/UseCases/Administration/KeyPointService.cs
using AutoMapper;
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public.Administration;
using Explorer.Tours.Core.Domain;
using FluentResults;

namespace Explorer.Tours.Core.UseCases.Administration
{
    public class KeyPointService : IKeyPointService
    {
        private readonly ICrudRepository<KeyPoint> _keyPointRepository;
        private readonly ICrudRepository<Tour> _tourRepository;
        private readonly IMapper _mapper;

        public KeyPointService(ICrudRepository<KeyPoint> keyPointRepository, ICrudRepository<Tour> tourRepository, IMapper mapper)
        {
            _keyPointRepository = keyPointRepository;
            _tourRepository = tourRepository;
            _mapper = mapper;
        }

        public Result<KeyPointDto> Create(KeyPointDto keyPointDto)
        {
            try
            {
                // Verify tour exists
                var tour = _tourRepository.Get(keyPointDto.TourId);
                if (tour == null)
                {
                    return Result.Fail(FailureCode.NotFound).WithError("Tour not found");
                }

                // Set order if not provided
                if (keyPointDto.Order == 0)
                {
                    var existingKeyPoints = _keyPointRepository.GetAll().Where(kp => kp.TourId == keyPointDto.TourId);
                    keyPointDto.Order = existingKeyPoints.Any() ? existingKeyPoints.Max(kp => kp.Order) + 1 : 1;
                }

                var keyPoint = new KeyPoint(
                    keyPointDto.TourId,
                    keyPointDto.Name,
                    keyPointDto.Description,
                    keyPointDto.Latitude,
                    keyPointDto.Longitude,
                    keyPointDto.ImageUrl,
                    keyPointDto.Order
                );

                var createdKeyPoint = _keyPointRepository.Create(keyPoint);
                return Result.Ok(_mapper.Map<KeyPointDto>(createdKeyPoint));
            }
            catch (KeyNotFoundException)
            {
                return Result.Fail(FailureCode.NotFound).WithError("Tour not found");
            }
            catch (ArgumentException ex)
            {
                return Result.Fail(FailureCode.InvalidArgument).WithError(ex.Message);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error creating key point: {ex.Message}");
            }
        }

        public Result<KeyPointDto> Update(KeyPointDto keyPointDto)
        {
            try
            {
                var existingKeyPoint = _keyPointRepository.Get(keyPointDto.Id);
                if (existingKeyPoint == null)
                {
                    return Result.Fail(FailureCode.NotFound);
                }

                existingKeyPoint.Update(
                    keyPointDto.Name,
                    keyPointDto.Description,
                    keyPointDto.Latitude,
                    keyPointDto.Longitude,
                    keyPointDto.ImageUrl,
                    keyPointDto.Order
                );

                var updatedKeyPoint = _keyPointRepository.Update(existingKeyPoint);
                return Result.Ok(_mapper.Map<KeyPointDto>(updatedKeyPoint));
            }
            catch (ArgumentException ex)
            {
                return Result.Fail(FailureCode.InvalidArgument).WithError(ex.Message);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error updating key point: {ex.Message}");
            }
        }

        public Result Delete(long id)
        {
            try
            {
                var keyPoint = _keyPointRepository.Get(id);
                if (keyPoint == null)
                {
                    return Result.Fail(FailureCode.NotFound);
                }

                _keyPointRepository.Delete(keyPoint.Id);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error deleting key point: {ex.Message}");
            }
        }

        public Result<KeyPointDto> Get(long id)
        {
            try
            {
                var keyPoint = _keyPointRepository.Get(id);
                if (keyPoint == null)
                {
                    return Result.Fail(FailureCode.NotFound);
                }

                return Result.Ok(_mapper.Map<KeyPointDto>(keyPoint));
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error getting key point: {ex.Message}");
            }
        }

        public Result<IEnumerable<KeyPointDto>> GetByTourId(long tourId)
        {
            try
            {
                var keyPoints = _keyPointRepository.GetAll()
                    .Where(kp => kp.TourId == tourId)
                    .OrderBy(kp => kp.Order)
                    .ToList();

                var keyPointDtos = _mapper.Map<IEnumerable<KeyPointDto>>(keyPoints);
                return Result.Ok(keyPointDtos);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error getting key points for tour: {ex.Message}");
            }
        }
    }
}
