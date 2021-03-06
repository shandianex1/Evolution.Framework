﻿/*******************************************************************************
 * Copyright © 2016 Evolution.Framework 版权所有
 * Author: Evolution
 * Description: Evolution快速开发平台
 * Website：
*********************************************************************************/
using Microsoft.AspNetCore.Http;
using Evolution.Domain.Entity.SystemManage;
using Evolution.Domain.IRepository.SystemManage;
using System;
using System.Collections.Generic;
using System.Linq;
using Evolution.Data;
using Evolution.Framework;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Evolution.Application.SystemManage
{
    public class MenuApp
    {
        #region 私有变量
        private IMenuRepository service = null;
        private IRoleAuthorizeRepository roleAuthRepo = null;
        private HttpContext context = null;
        #endregion
        #region 构造函数
        public MenuApp(IMenuRepository service,IRoleAuthorizeRepository roleAuthRepo,IHttpContextAccessor contextAccessor)
        {
            this.service = service;
            this.roleAuthRepo = roleAuthRepo;
            this.context = contextAccessor.HttpContext;
        }
        #endregion
        /// <summary>
        /// 根据角色获取菜单条目
        /// </summary>
        /// <param name="roleId">角色Id</param>
        /// <returns></returns>
        public async Task<List<MenuEntity>> GetMenuListByRoleId(string roleId)
        {
            var data = new List<MenuEntity>();
            var isSystem = context.User.Claims.FirstOrDefault(t => t.Type == OperatorModelClaimNames.IsSystem).Value;
            if (isSystem == null) return null;
            if (isSystem.ToBool())
            {
                data = await this.GetList();
            }
            else
            {
                var menuData = await this.GetList();
                var authmenudata = await roleAuthRepo.IQueryable(t => t.ObjectId == roleId && t.ItemType == 1).ToListAsync();
                foreach (var item in authmenudata)
                {
                    MenuEntity moduleEntity = menuData.Find(t => t.Id == item.ItemId);
                    if (moduleEntity != null)
                        data.Add(moduleEntity);
                }
            }
            return data.OrderBy(t => t.SortCode).ToList();
        }
        /// <summary>
        /// 获取所有菜单列表
        /// </summary>
        /// <returns></returns>
        public Task<List<MenuEntity>> GetList()
        {
            return service.IQueryable().OrderBy(t => t.SortCode).ToListAsync();
        }
        /// <summary>
        /// 通过菜单Id获取菜单
        /// </summary>
        /// <param name="keyValue">菜单Id</param>
        /// <returns></returns>
        public Task<MenuEntity> GetMenuById(string keyValue)
        {
            return service.FindEntityAsync(keyValue);
        }
        /// <summary>
        /// 删除菜单，若有父菜单，则禁止删除
        /// </summary>
        /// <param name="keyValue">菜单Id</param>
        public Task<int> Delete(string keyValue)
        {
            if (service.IQueryable().Count(t => t.ParentId.Equals(keyValue)) > 0)
            {
                throw new Exception("删除失败！操作的对象包含了下级数据。");
            }
            else
            {
                return service.DeleteAsync(t => t.Id == keyValue);
            }
        }
        /// <summary>
        /// 保存菜单
        /// </summary>
        /// <param name="menuEntity">菜单实体</param>
        /// <param name="keyValue">菜单Id，有id则更新，无id则新建</param>
        public Task<int> Save(MenuEntity menuEntity, string keyValue)
        {
            if (!string.IsNullOrEmpty(keyValue))
            {
                menuEntity.AttachModifyInfo(keyValue, context);
                return service.UpdateAsync(menuEntity);
            }
            else
            {
                menuEntity.AttachCreateInfo(context);
                return service.InsertAsync(menuEntity);
            }
        }
    }
}
