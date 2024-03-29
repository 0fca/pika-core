﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pika.Domain.Status.Data;
using PikaCore.Areas.Core.Data;

namespace PikaCore.Infrastructure.Services
{
    public class SystemService : ISystemService
    {
        private readonly StorageIndexContext _systemContext;

        public SystemService(StorageIndexContext systemContext)
        {
            _systemContext = systemContext;
        }

        public async Task<SystemDescriptor> GetDescriptorByName(string name)
        {
            return await _systemContext.Systems.FirstAsync(s => s.SystemName.Equals(name));
        }

        public async Task<SystemDescriptor> GetDescriptorById(int id)
        {
            return await _systemContext.Systems.FirstAsync(s => s.SystemId == id);
        }

        public async Task<string> GetNameById(int id)
        {
            return (await GetDescriptorById(id)).SystemName;
        }

        public async Task<int> GetIdByName(string name)
        {
            return (await GetDescriptorByName(name)).SystemId;
        }

        public async Task<List<string>> GetAllSystemNames()
        {
            return (await GetAll()).Select(s => s.SystemName).ToList();
        }

        public async Task<List<SystemDescriptor>> GetAll()
        {
            return await _systemContext.Systems.ToListAsync();
        }
    }
}