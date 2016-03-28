using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Spaicial_API.Models;

namespace Spaicial_API.Controllers
{
    public class StationDataController : ApiController
    {
        private spaicial_dbEntities db = new spaicial_dbEntities();

        //// GET: api/StationData
        //public IQueryable<StationData> GetStationData()
        //{
        //    return db.StationData;
        //}

        //// GET: api/StationData/5
        //[ResponseType(typeof(StationData))]
        //public async Task<IHttpActionResult> GetStationData(int id)
        //{
        //    StationData stationData = await db.StationData.FindAsync(id);
        //    if (stationData == null)
        //    {
        //        return NotFound();
        //    }

        //    return Ok(stationData);
        //}

        //// PUT: api/StationData/5
        //[ResponseType(typeof(void))]
        //public async Task<IHttpActionResult> PutStationData(int id, StationData stationData)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    if (id != stationData.stationDataId)
        //    {
        //        return BadRequest();
        //    }

        //    db.Entry(stationData).State = EntityState.Modified;

        //    try
        //    {
        //        await db.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!StationDataExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return StatusCode(HttpStatusCode.NoContent);
        //}

        // POST: api/StationData
        [ResponseType(typeof(StationData))]
        public async Task<IHttpActionResult> PostStationData(StationDataCollector stationDataCollector)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            int apiKeyId = db.ApiKey.Where(a => a.keyValue == stationDataCollector.apiKey).First().apiKeyId;

            StationData stationData = stationDataCollector.ConvertToDb(apiKeyId,db);

            db.StationData.Add(stationData);
            await db.SaveChangesAsync();

            return CreatedAtRoute("DefaultApi", new { id = stationData.stationDataId }, stationDataCollector);
        }

        //// DELETE: api/StationData/5
        //[ResponseType(typeof(StationData))]
        //public async Task<IHttpActionResult> DeleteStationData(int id)
        //{
        //    StationData stationData = await db.StationData.FindAsync(id);
        //    if (stationData == null)
        //    {
        //        return NotFound();
        //    }

        //    db.StationData.Remove(stationData);
        //    await db.SaveChangesAsync();

        //    return Ok(stationData);
        //}

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool StationDataExists(int id)
        {
            return db.StationData.Count(e => e.stationDataId == id) > 0;
        }
    }
}