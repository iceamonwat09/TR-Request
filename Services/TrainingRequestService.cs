using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TrainingRequestApp.Models;

namespace TrainingRequestApp.Services
{
    public interface ITrainingRequestService
    {
        Task<TrainingRequest> CreateTrainingRequestAsync(TrainingRequest trainingRequest);
        Task<TrainingRequest?> GetTrainingRequestByIdAsync(int id);
        Task<List<TrainingRequest>> GetAllTrainingRequestsAsync();
        Task<bool> UpdateTrainingRequestAsync(TrainingRequest trainingRequest);
        Task<bool> DeleteTrainingRequestAsync(int id);
        Task<bool> AddParticipantAsync(int trainingRequestId, TrainingParticipant participant);
        Task<bool> RemoveParticipantAsync(int trainingRequestId, string userId);
    }

    public class TrainingRequestService : ITrainingRequestService
    {
        private readonly ApplicationDbContext _context;

        public TrainingRequestService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TrainingRequest> CreateTrainingRequestAsync(TrainingRequest trainingRequest)
        {
            trainingRequest.CreatedDate = DateTime.Now;
            trainingRequest.Status = "Pending";

            _context.TrainingRequests.Add(trainingRequest);
            await _context.SaveChangesAsync();
            return trainingRequest;
        }

        public async Task<TrainingRequest?> GetTrainingRequestByIdAsync(int id)
        {
            return await _context.TrainingRequests
                .Include(tr => tr.Participants)
                .FirstOrDefaultAsync(tr => tr.Id == id);
        }

        public async Task<List<TrainingRequest>> GetAllTrainingRequestsAsync()
        {
            return await _context.TrainingRequests
                .Include(tr => tr.Participants)
                .OrderByDescending(tr => tr.CreatedDate)
                .ToListAsync();
        }

        public async Task<bool> UpdateTrainingRequestAsync(TrainingRequest trainingRequest)
        {
            try
            {
                _context.TrainingRequests.Update(trainingRequest);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteTrainingRequestAsync(int id)
        {
            try
            {
                var trainingRequest = await _context.TrainingRequests.FindAsync(id);
                if (trainingRequest == null)
                    return false;

                _context.TrainingRequests.Remove(trainingRequest);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AddParticipantAsync(int trainingRequestId, TrainingParticipant participant)
        {
            try
            {
                participant.TrainingRequestId = trainingRequestId;
                participant.AddedDate = DateTime.Now;

                // ตรวจสอบว่าพนักงานนี้มีในรายการแล้วหรือไม่
                var existingParticipant = await _context.TrainingParticipants
                    .FirstOrDefaultAsync(p => p.TrainingRequestId == trainingRequestId && p.UserID == participant.UserID);

                if (existingParticipant != null)
                    return false; // พนักงานนี้มีอยู่แล้ว

                _context.TrainingParticipants.Add(participant);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoveParticipantAsync(int trainingRequestId, string userId)
        {
            try
            {
                var participant = await _context.TrainingParticipants
                    .FirstOrDefaultAsync(p => p.TrainingRequestId == trainingRequestId && p.UserID == userId);

                if (participant == null)
                    return false;

                _context.TrainingParticipants.Remove(participant);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}