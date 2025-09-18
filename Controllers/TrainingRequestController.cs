using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TrainingRequestApp.Models;
using TrainingRequestApp.Services;

namespace TrainingRequestApp.Controllers
{
    public class TrainingRequestController : Controller
    {
        private readonly ITrainingRequestService _trainingRequestService;
        private readonly IEmployeeService _employeeService;

        public TrainingRequestController(ITrainingRequestService trainingRequestService, IEmployeeService employeeService)
        {
            _trainingRequestService = trainingRequestService;
            _employeeService = employeeService;
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string TrainingTitle, DateTime TrainingDate, string Location, string ParticipantsJson)
        {
            try
            {
                var trainingRequest = new TrainingRequest
                {
                    TrainingTitle = TrainingTitle,
                    TrainingDate = TrainingDate,
                    Location = Location
                };

                var createdRequest = await _trainingRequestService.CreateTrainingRequestAsync(trainingRequest);

                // เพิ่มผู้เข้าร่วม
                if (!string.IsNullOrWhiteSpace(ParticipantsJson))
                {
                    var participants = System.Text.Json.JsonSerializer.Deserialize<List<ParticipantViewModel>>(ParticipantsJson);
                    
                    if (participants != null)
                    {
                        foreach (var participantModel in participants)
                        {
                            var participant = new TrainingParticipant
                            {
                                UserID = participantModel.UserID,
                                Prefix = participantModel.Prefix,
                                Name = participantModel.Name,
                                Lastname = participantModel.Lastname,
                                Level = participantModel.Level
                            };

                            await _trainingRequestService.AddParticipantAsync(createdRequest.Id, participant);
                        }
                    }
                }

                TempData["SuccessMessage"] = "สร้างคำขอฝึกอบรมเรียบร้อยแล้ว";
                return RedirectToAction(nameof(Details), new { id = createdRequest.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "เกิดข้อผิดพลาดในการบันทึกข้อมูล: " + ex.Message);
                ViewBag.TrainingTitle = TrainingTitle;
                ViewBag.TrainingDate = TrainingDate;
                ViewBag.Location = Location;
                return View();
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var trainingRequest = await _trainingRequestService.GetTrainingRequestByIdAsync(id);
            if (trainingRequest == null)
            {
                return NotFound();
            }

            return View(trainingRequest);
        }

        public async Task<IActionResult> Index()
        {
            var trainingRequests = await _trainingRequestService.GetAllTrainingRequestsAsync();
            return View(trainingRequests);
        }

        [HttpPost]
        public async Task<IActionResult> AddParticipant(int trainingRequestId, string userId)
        {
            var employee = await _employeeService.GetEmployeeByUserIdAsync(userId);
            if (employee == null)
            {
                return Json(new { success = false, message = "ไม่พบข้อมูลพนักงาน" });
            }

            var participant = new TrainingParticipant
            {
                UserID = employee.UserID ?? "",
                Prefix = employee.Prefix,
                Name = employee.Name,
                Lastname = employee.Lastname,
                Level = employee.Level
            };

            var result = await _trainingRequestService.AddParticipantAsync(trainingRequestId, participant);
            
            if (result)
            {
                return Json(new { success = true, message = "เพิ่มผู้เข้าร่วมเรียบร้อยแล้ว" });
            }
            else
            {
                return Json(new { success = false, message = "ไม่สามารถเพิ่มผู้เข้าร่วมได้ (อาจมีอยู่แล้ว)" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveParticipant(int trainingRequestId, string userId)
        {
            var result = await _trainingRequestService.RemoveParticipantAsync(trainingRequestId, userId);
            
            if (result)
            {
                return Json(new { success = true, message = "ลบผู้เข้าร่วมเรียบร้อยแล้ว" });
            }
            else
            {
                return Json(new { success = false, message = "ไม่สามารถลบผู้เข้าร่วมได้" });
            }
        }
    }

    public class CreateTrainingRequestViewModel
    {
        public string TrainingTitle { get; set; } = string.Empty;
        public DateTime TrainingDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public List<ParticipantViewModel>? Participants { get; set; }
    }

    public class ParticipantViewModel
    {
        public string UserID { get; set; } = string.Empty;
        public string? Prefix { get; set; }
        public string? Name { get; set; }
        public string? Lastname { get; set; }
        public string? Level { get; set; }
    }
}