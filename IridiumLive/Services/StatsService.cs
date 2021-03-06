﻿/*
 * microp11 2019
 * 
 * This file is part of IridiumLive.
 * 
 * IridiumLive is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * IridiumLive is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with IridiumLive.  If not, see <http://www.gnu.org/licenses/>.
 *
 *
 */

using IridiumLive.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IridiumLive.Services
{
    public interface IStatsService
    {
        public Task<ICollection<Stat>> GetStatsAsync();
        public Task<ICollection<Stat>> GetStatsAsync(DateTime from, DateTime to);
    }

    public class StatsService : IridiumService, IStatsService
    {
        public StatsService(IConfiguration configuration) : base(configuration) { }

        public async Task<ICollection<Stat>> GetStatsAsync()
        {
            using IridiumLiveDbContext _context = new IridiumLiveDbContext(Options);
            FormattableString sqlString = $@"
                select s.SatNo, IFNUll(x.Count, 0) Iras, IFNULL(y.Count, 0) Ibcs 
                from Sats s
                left outer join(select a.SatNo, count(*) Count from Iras a group by a.SatNo) x on s.SatNo = x.SatNo
                left outer join(select b.SatNo, count(*) Count from Ibcs b group by b.SatNo) y on s.SatNo = y.SatNo
                order by s.SatNo";
            return await _context.Stats
                .FromSqlInterpolated(sqlString)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ICollection<Stat>> GetStatsAsync(DateTime from, DateTime to)
        {
            //this should be changed fundamentally, no need for these conversions
            from = DateTime.SpecifyKind(from, DateTimeKind.Local);
            DateTimeOffset fromOffset = new DateTimeOffset(from);
            long fromUtcTicks = fromOffset.UtcTicks;

            to = DateTime.SpecifyKind(to, DateTimeKind.Local);
            DateTimeOffset toOffset = new DateTimeOffset(to);
            long toUtcTicks = toOffset.UtcTicks;

            using IridiumLiveDbContext _context = new IridiumLiveDbContext(Options);
            FormattableString sqlString = $@"
                select s.SatNo, IFNUll(x.Count, 0) Iras, IFNULL(y.Count, 0) Ibcs 
                from Sats s
                left outer join (
                    select a.SatNo, a.UtcTicks, count(*) Count from Iras a
                    where a.UtcTicks >= {fromUtcTicks} and a.UtcTicks <= {toUtcTicks}
                    group by a.SatNo
                    ) x on s.SatNo = x.SatNo
                left outer join (
                    select b.SatNo, b.UtcTicks, count(*) Count from Ibcs b
                    where b.UtcTicks >= {fromUtcTicks} and b.UtcTicks <= {toUtcTicks}
                    group by b.SatNo
                    ) y on s.SatNo = y.SatNo
                order by s.SatNo";
            return await _context.Stats
                .FromSqlInterpolated(sqlString)
                .Where(z => z.Iras > 0 || z.Ibcs > 0)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
