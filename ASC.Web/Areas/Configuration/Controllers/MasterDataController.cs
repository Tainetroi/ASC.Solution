using ASC.Business.Interfaces;
using ASC.Model.Models;
using ASC.Utilities;
using ASC.Web.Areas.Configuration.Models;
using ASC.Web.Controllers;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ASC.Web.Areas.Configuration.Controllers
{
    [Area("Configuration")]
    [Authorize(Roles = "Admin")]
    public class MasterDataController : BaseController
    {
        private readonly IMasterDataOperations _masterData;
        private readonly IMapper _mapper;

        public MasterDataController(IMasterDataOperations masterData, IMapper mapper)
        {
            _masterData = masterData;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> MasterKeys()
        {
            var masterKeys = await _masterData.GetAllMasterKeysAsync();
            var viewModels = _mapper.Map<List<MasterDataKey>, List<MasterDataKeyViewModel>>(masterKeys);

            HttpContext.Session.SetSession("MasterKeys", viewModels);

            var model = new MasterKeysViewModel
            {
                MasterKeys = viewModels.ToList(),
                IsEdit = false,
                MasterKeyInContext = new MasterDataKeyViewModel()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MasterKeys(MasterKeysViewModel model)
        {
            model.MasterKeys = HttpContext.Session.GetSession<List<MasterDataKeyViewModel>>("MasterKeys");

            if (!ModelState.IsValid)
                return View(model);

            var entity = _mapper.Map<MasterDataKeyViewModel, MasterDataKey>(model.MasterKeyInContext);

            try
            {
                if (model.IsEdit)
                {
                    // Thêm UpdatedBy khi cập nhật
                    entity.UpdatedBy = HttpContext.User.GetCurrentUserDetails().Name;
                    await _masterData.UpdateMasterKeyAsync(model.MasterKeyInContext.PartitionKey, entity);
                }
                else
                {
                    entity.RowKey = Guid.NewGuid().ToString();
                    entity.PartitionKey = entity.Name;
                    entity.CreatedBy = HttpContext.User.GetCurrentUserDetails().Name;
                    entity.UpdatedBy = HttpContext.User.GetCurrentUserDetails().Name; // Cung cấp UpdatedBy khi thêm mới

                    await _masterData.InsertMasterKeyAsync(entity);
                }

            }
            catch (Exception ex)
            {
                var errorMsg = ex.InnerException?.Message ?? ex.Message;
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi: " + errorMsg);
                return View(model);
            }

            return RedirectToAction("MasterKeys");
        }

        [HttpGet]
        public async Task<IActionResult> MasterValues()
        {
            ViewBag.MasterKeys = await _masterData.GetAllMasterKeysAsync() ?? new List<MasterDataKey>();
            return View(new MasterValuesViewModel
            {
                MasterValues = new List<MasterDataValueViewModel>(),
                IsEdit = false
            });
        }

        [HttpGet]
        public async Task<IActionResult> MasterValuesByKey(string key)
        {
            var values = await _masterData.GetAllMasterValuesByKeyAsync(key);
            return Json(new { data = values });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveMasterValue(bool isEdit, MasterDataValueViewModel masterValue)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, errors });
            }

            try
            {
                var masterDataValue = _mapper.Map<MasterDataValueViewModel, MasterDataValue>(masterValue);

                if (isEdit)
                {
                    await _masterData.UpdateMasterValueAsync(masterDataValue.PartitionKey, masterDataValue.RowKey, masterDataValue);
                }
                else
                {
                    masterDataValue.RowKey = Guid.NewGuid().ToString();
                    await _masterData.InsertMasterValueAsync(masterDataValue);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
            }
        }
    }
}
