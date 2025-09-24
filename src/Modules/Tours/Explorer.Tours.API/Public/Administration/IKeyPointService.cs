// src/Modules/Tours/Explorer.Tours.API/Public/Administration/IKeyPointService.cs
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Dtos;
using FluentResults;

namespace Explorer.Tours.API.Public.Administration
{
    public interface IKeyPointService
    {
        Result<KeyPointDto> Create(KeyPointDto keyPoint);
        Result<KeyPointDto> Update(KeyPointDto keyPoint);
        Result Delete(long id);
        Result<KeyPointDto> Get(long id);
        Result<IEnumerable<KeyPointDto>> GetByTourId(long tourId);
    }
}
