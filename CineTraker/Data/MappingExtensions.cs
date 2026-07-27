using CineTraker.Data.Entities;
using CineTraker.Shared;
using CineTraker.Shared.Models;
using System.Linq;
using System.Collections.Generic;

namespace CineTraker.Data
{
    public static class MappingExtensions
    {
        // Movie
        public static Movie? ToDto(this MovieEntity? entity)
        {
            if (entity == null) return null;
            return new Movie
            {
                Id = entity.Id,
                Title = entity.Title,
                Year = entity.Year,
                Director = entity.Director,
                PosterUrl = entity.PosterUrl,
                Plot = entity.Plot,
                ImdbID = entity.ImdbID,
                Genre = entity.Genre,
                Runtime = entity.Runtime,
                Actors = entity.Actors,
                Rated = entity.Rated,
                PlotEmbedding = entity.PlotEmbedding,
                ImdbRating = entity.ImdbRating,
                Sources = entity.Sources?.Select(s => s.ToDto()).Where(s => s != null).Cast<StreamingSource>().ToList() ?? new List<StreamingSource>()
            };
        }

        public static MovieEntity? ToEntity(this Movie? dto)
        {
            if (dto == null) return null;
            return new MovieEntity
            {
                Id = dto.Id,
                Title = dto.Title,
                Year = dto.Year,
                Director = dto.Director,
                PosterUrl = dto.PosterUrl,
                Plot = dto.Plot,
                ImdbID = dto.ImdbID,
                Genre = dto.Genre,
                Runtime = dto.Runtime,
                Actors = dto.Actors,
                Rated = dto.Rated,
                PlotEmbedding = dto.PlotEmbedding,
                ImdbRating = dto.ImdbRating,
                Sources = dto.Sources?.Select(s => s.ToEntity()).Where(s => s != null).Cast<StreamingSourceEntity>().ToList() ?? new List<StreamingSourceEntity>()
            };
        }

        // Review
        public static Review? ToDto(this ReviewEntity? entity)
        {
            if (entity == null) return null;
            return new Review
            {
                Id = entity.Id,
                Stars = entity.Stars,
                Comment = entity.Comment,
                CreatedAt = entity.CreatedAt,
                MovieId = entity.MovieId,
                UserId = entity.UserId,
                Movie = entity.Movie?.ToDto()
            };
        }

        public static ReviewEntity? ToEntity(this Review? dto)
        {
            if (dto == null) return null;
            return new ReviewEntity
            {
                Id = dto.Id,
                Stars = dto.Stars,
                Comment = dto.Comment,
                CreatedAt = dto.CreatedAt,
                MovieId = dto.MovieId,
                UserId = dto.UserId
            };
        }

        // MovieRequest
        public static MovieRequest? ToDto(this MovieRequestEntity? entity)
        {
            if (entity == null) return null;
            return new MovieRequest
            {
                Id = entity.Id,
                ImdbID = entity.ImdbID,
                Title = entity.Title,
                Year = entity.Year,
                PosterUrl = entity.PosterUrl,
                RequestedByUserId = entity.RequestedByUserId,
                RequestedByUsername = entity.RequestedByUsername,
                RequestedAt = entity.RequestedAt,
                Status = entity.Status
            };
        }

        public static MovieRequestEntity? ToEntity(this MovieRequest? dto)
        {
            if (dto == null) return null;
            return new MovieRequestEntity
            {
                Id = dto.Id,
                ImdbID = dto.ImdbID,
                Title = dto.Title,
                Year = dto.Year,
                PosterUrl = dto.PosterUrl,
                RequestedByUserId = dto.RequestedByUserId,
                RequestedByUsername = dto.RequestedByUsername,
                RequestedAt = dto.RequestedAt,
                Status = dto.Status
            };
        }

        // StreamingSource
        public static StreamingSource? ToDto(this StreamingSourceEntity? entity)
        {
            if (entity == null) return null;
            return new StreamingSource
            {
                Id = entity.Id,
                Name = entity.Name,
                Type = entity.Type,
                WebUrl = entity.WebUrl,
                LogoUrl = entity.LogoUrl
            };
        }

        public static StreamingSourceEntity? ToEntity(this StreamingSource? dto)
        {
            if (dto == null) return null;
            return new StreamingSourceEntity
            {
                Id = dto.Id,
                Name = dto.Name,
                Type = dto.Type,
                WebUrl = dto.WebUrl,
                LogoUrl = dto.LogoUrl
            };
        }

        // UserMap
        public static UserMap? ToDto(this UserMapEntity? entity)
        {
            if (entity == null) return null;
            return new UserMap
            {
                Id = entity.Id,
                Name = entity.Name,
                CreatedDate = entity.CreatedDate,
                UserId = entity.UserId,
                GraphJson = entity.GraphJson,
                SeedMovieId = entity.SeedMovieId,
                TotalMovies = entity.TotalMovies,
                WatchedMovies = entity.WatchedMovies,
                SeedMovie = entity.SeedMovie?.ToDto()
            };
        }

        public static UserMapEntity? ToEntity(this UserMap? dto)
        {
            if (dto == null) return null;
            return new UserMapEntity
            {
                Id = dto.Id,
                Name = dto.Name,
                CreatedDate = dto.CreatedDate,
                UserId = dto.UserId,
                GraphJson = dto.GraphJson,
                SeedMovieId = dto.SeedMovieId,
                TotalMovies = dto.TotalMovies,
                WatchedMovies = dto.WatchedMovies
            };
        }
    }
}
