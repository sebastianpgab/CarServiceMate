﻿using AutoMapper;
using Microsoft.EntityFrameworkCore;
using CarServiceMate.Authorization;
using CarServiceMate.Entities;
using CarServiceMate.Exceptions;
using CarServiceMate.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CarServiceMate.Services
{
    public interface IVehicleService
    {
        public IEnumerable<VehicleDto> GetAll();
        public VehicleDto GetById(int id);
        public int CreateVehicle(VehicleDto vehicleDto, int clientId);
        public int Delete(int id, ClaimsPrincipal user);
        public VehicleDto Update(int id, VehicleDto vehicle, ClaimsPrincipal user);
        public Client FindClient(int id);
        public Task<Vehicle> SearchVin(string searchedVin);
        public Task<IEnumerable<Vehicle>> SearchName(string name);

    }
    public class VehicleService : IVehicleService
     {
         private readonly IMapper _autoMapper;
         private readonly CarServiceMateDbContext _dbContext;
         private readonly ILogger<VehicleService> _logger;
         private readonly IAuthorizationService _authorizationService;
         public VehicleService(IMapper autoMapper, CarServiceMateDbContext dbContext, ILogger<VehicleService> logger,
             IAuthorizationService authorizationService)
         {
             _autoMapper = autoMapper;
             _dbContext = dbContext;
             _logger = logger;
             _authorizationService = authorizationService;
         }

         public IEnumerable<VehicleDto> GetAll()
         {
             var vehicles = _dbContext.Vehicles.ToList();
             if(vehicles is not null)
             {
                 var vehiclesDtos = _autoMapper.Map<List<VehicleDto>>(vehicles);
                 return vehiclesDtos;
             }
             throw new NotFoundException("Vehicles not found");
         }

         public VehicleDto GetById(int id)
         {
             var vehicle = _dbContext.Vehicles.FirstOrDefault(p => p.Id == id);
             if(vehicle is not null)
             {
                 var vehicleDto = _autoMapper.Map<VehicleDto>(vehicle);
                 return vehicleDto;
             }
             throw new NotFoundException("Vehicle not found");
         }

         public int CreateVehicle(VehicleDto vehicleDto, int clientId)
         {
             if(vehicleDto is not null)
             {
                 var vehicle = _autoMapper.Map<Vehicle>(vehicleDto);
                 vehicle.ClientId = clientId;
                 _dbContext.Vehicles.Add(vehicle);
                 _dbContext.SaveChanges();
                 return vehicle.Id;
             }
             throw new NotFoundException("Vehicle not found");
         }

         public int Delete(int id, ClaimsPrincipal user)
         {
             _logger.LogWarning($"Vehicle with id: {id} DELETE action invoked");
             var vehicle = _dbContext.Vehicles.FirstOrDefault(p => p.Id == id);
             if(vehicle is not null)
             {
                 var authorizationResult = _authorizationService.AuthorizeAsync(user, vehicle, new ResourceOperationRequirement(ResourceOperation.Delete)).Result;

                 if (!authorizationResult.Succeeded)
                 {
                     throw new ForbidException();
                 }
                 _dbContext.Vehicles.Remove(vehicle);
                 _dbContext.SaveChanges();
                 return id;
             }
             throw new NotFoundException("Vehicle not found");


         }

         public VehicleDto Update(int id, VehicleDto vehicleDto, ClaimsPrincipal user)
         {
             var vehicle = _dbContext.Vehicles.FirstOrDefault(p => p.Id == id);
             if(vehicle is not null)
             {
                /* AUTORYZACJA ODPOWIEDZIALNA ZA EDYCJE JESLI SIĘ POWIEDZIE LOGOWANIE TO ZEZWOLI NA UPDATE DANYCH (NA RAZIE OFF)
                 var authorizationResult = _authorizationService.AuthorizeAsync(user, vehicle, new ResourceOperationRequirement(ResourceOperation.Update)).Result;

                 if (!authorizationResult.Succeeded)
                 {
                     throw new ForbidException();
                 }
                */

                 vehicle.Make = vehicleDto.Make is not null && vehicleDto.Make.Length <= 25 ? vehicleDto.Make : vehicle.Make;
                 vehicle.Model = vehicleDto.Model is not null && vehicleDto.Model.Length <= 25  ? vehicleDto.Model : vehicle.Model;
                 vehicle.VIN = vehicleDto.VIN is not null && vehicleDto.VIN.Length <= 17 ? vehicleDto.VIN : vehicle.Model;
                 vehicle.Year = vehicleDto.Year != 0 && vehicleDto.Year.ToString().Length == 4 ? vehicleDto.Year : vehicle.Year;
                 vehicle.Engine = vehicleDto.Engine is not null && vehicleDto.Engine.Length <= 4 ? vehicleDto.Engine : vehicle.Model;
                 vehicle.Kilometers = vehicleDto.Kilometers != 0 ? vehicleDto.Kilometers : vehicle.Kilometers;
                 vehicle.Status = vehicleDto.Status is not null ? vehicleDto.Status : vehicle.Status;

                 var vehicleMaped = _autoMapper.Map<VehicleDto>(vehicle);

                _dbContext.SaveChanges();
                 return vehicleMaped;
             }
             throw new NotFoundException("Vehicle not found");
         }

        //zastanowić się czy ta metoda nie powinna być w ClientService
         public Client FindClient(int clientId)
         {
             var client = _dbContext.Clients.FirstOrDefault(p => p.Id == clientId);

             if (client is not null)
             {
                 return client;
             }
             throw new NotFoundException("Client does not exsit");
         }

         public async Task<Vehicle> SearchVin(string searchedVin)
         {
            var foundVehicle = await _dbContext.Vehicles.FirstOrDefaultAsync(p => p.VIN == searchedVin);
            return foundVehicle;

         }

        public async Task <IEnumerable<Vehicle>> SearchName(string name)
        {
            string[] parts = name.Split(' ');
            string firstname = parts[0].Trim().ToLower();
            string lastname = parts.Length > 1 ? parts[1].Trim().ToLower() : null;
            List<Vehicle> vehicles = new List<Vehicle>();

            var clients = _dbContext.Clients.Where(p => p.FirstName == firstname && p.LastName == lastname);

            if (await clients.AnyAsync())
            {
                var clientsId = await clients.Select(p => p.Id).ToListAsync();

                foreach (var id in clientsId)
                {
                    var vehicle = _dbContext.Vehicles.Where(c => c.ClientId == id);
                    vehicles.AddRange(vehicle);
                }
                return vehicles;
            }
            return vehicles;
        }

    }
}