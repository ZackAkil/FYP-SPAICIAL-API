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
    public class ScoutDataController : ApiController
    {
        private spaicial_dbEntities db = new spaicial_dbEntities();

        //// GET: api/ScoutData
        //public IQueryable<ScoutData> GetScoutData()
        //{
        //    return db.ScoutData;
        //}

        //// GET: api/ScoutData/5
        //[ResponseType(typeof(ScoutData))]
        //public async Task<IHttpActionResult> GetScoutData(int id)
        //{
        //    ScoutData scoutData = await db.ScoutData.FindAsync(id);
        //    if (scoutData == null)
        //    {
        //        return NotFound();
        //    }

        //    return Ok(scoutData);
        //}

        // // PUT: api/ScoutData/5
        //[ResponseType(typeof(void))]
        //public async Task<IHttpActionResult> PutScoutData(int id, ScoutData scoutData)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }
        //
        //    if (id != scoutData.scoutDataId)
        //    {
        //        return BadRequest();
        //    }
        //
        //    db.Entry(scoutData).State = EntityState.Modified;
        //
        //    try
        //    {
        //        await db.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!ScoutDataExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }
        //
        //    return StatusCode(HttpStatusCode.NoContent);
        //}

        // POST: api/ScoutData
        [ResponseType(typeof(ScoutData))]
        public async Task<IHttpActionResult> PostScoutData(ScoutDataCollector scoutDataCollector)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            int apiKeyId = db.ApiKey.Where(a => a.keyValue == scoutDataCollector.apiKey).First().apiKeyId;

            ScoutData scoutData = scoutDataCollector.ConvertToDb(apiKeyId, db);

            db.ScoutData.Add(scoutData);
            await db.SaveChangesAsync();

            return CreatedAtRoute("DefaultApi", new { id = scoutData.scoutDataId }, scoutDataCollector);
        }

        //// DELETE: api/ScoutData/5
        //[ResponseType(typeof(ScoutData))]
        //public async Task<IHttpActionResult> DeleteScoutData(int id)
        //{
        //    ScoutData scoutData = await db.ScoutData.FindAsync(id);
        //    if (scoutData == null)
        //    {
        //        return NotFound();
        //    }

        //    db.ScoutData.Remove(scoutData);
        //    await db.SaveChangesAsync();

        //    return Ok(scoutData);
        //}

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool ScoutDataExists(int id)
        {
            return db.ScoutData.Count(e => e.scoutDataId == id) > 0;
        }
    }
}