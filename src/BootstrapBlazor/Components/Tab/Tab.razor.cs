﻿// Copyright (c) Argo Zhang (argo@163.com). All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Website: https://www.blazor.zone or https://argozhang.github.io/

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BootstrapBlazor.Components
{
    /// <summary>
    /// Tab 组件基类
    /// </summary>
    public sealed partial class Tab
    {
        static ConcurrentDictionary<string, Type> RouteTable { get; set; } = new ConcurrentDictionary<string, Type>();

        /// <summary>
        /// 
        /// </summary>
        private bool FirstRender { get; set; } = true;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private static string? GetContentClassString(TabItem item) => CssBuilder.Default("tabs-body-content")
            .AddClass("d-none", !item.IsActive)
            .Build();

        private string? WrapClassString => CssBuilder.Default("tabs-nav-wrap")
            .AddClass("extend", ShouldShowExtendButtons())
            .Build();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="active"></param>
        /// <returns></returns>
        private string? GetClassString(bool active) => CssBuilder.Default("tabs-item")
            .AddClass("active", active)
            .AddClass("is-closeable", ShowClose)
            .Build();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="icon"></param>
        /// <returns></returns>
        private static string? GetIconClassString(string icon) => CssBuilder.Default("fa fa-fw")
            .AddClass(icon)
            .Build();

        /// <summary>
        /// 获得/设置 Tab 组件 DOM 实例
        /// </summary>
        private ElementReference TabElement { get; set; }

        /// <summary>
        /// 获得 Tab 组件样式
        /// </summary>
        private string? ClassString => CssBuilder.Default("tabs")
            .AddClass("tabs-card", IsCard)
            .AddClass("tabs-border-card", IsBorderCard)
            .AddClass($"tabs-{Placement.ToDescriptionString()}", Placement == Placement.Top || Placement == Placement.Right || Placement == Placement.Bottom || Placement == Placement.Left)
            .AddClassFromAttributes(AdditionalAttributes)
            .Build();

        /// <summary>
        /// 获得 Tab 组件 Style
        /// </summary>
        private string? StyleString => CssBuilder.Default()
            .AddClass($"height: {Height}px;", Height > 0)
            .Build();

        private readonly List<TabItem> _items = new List<TabItem>(50);

        /// <summary>
        /// 获得/设置 TabItem 集合
        /// </summary>
        public IEnumerable<TabItem> Items => _items;

        /// <summary>
        /// 获得/设置 是否为卡片样式
        /// </summary>
        [Parameter]
        public bool IsCard { get; set; }

        /// <summary>
        /// 获得/设置 是否为带边框卡片样式
        /// </summary>
        [Parameter]
        public bool IsBorderCard { get; set; }

        /// <summary>
        /// 获得/设置 组件高度 默认值为 0 高度自动
        /// </summary>
        [Parameter]
        public int Height { get; set; }

        /// <summary>
        /// 获得/设置 组件标签显示位置 默认显示在 Top 位置
        /// </summary>
        [Parameter]
        public Placement Placement { get; set; } = Placement.Top;

        /// <summary>
        /// 获得/设置 是否显示关闭按钮 默认为 false 不显示
        /// </summary>
        [Parameter]
        public bool ShowClose { get; set; }

        /// <summary>
        /// 获得/设置 是否显示扩展功能按钮 默认为 false 不显示
        /// </summary>
        [Parameter]
        public bool ShowExtendButtons { get; set; }

        /// <summary>
        /// 获得/设置 点击 TabItem 时是否自动导航 默认为 false 不导航
        /// </summary>
        [Parameter]
        public bool ClickTabToNavigation { get; set; }

        /// <summary>
        /// 获得/设置 TabItems 模板
        /// </summary>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// 获得/设置 Gets or sets a collection of additional assemblies that should be searched for components that can match URIs.
        /// </summary>
        [Parameter]
        public IEnumerable<Assembly>? AdditionalAssemblies { get; set; }

        /// <summary>
        /// 获得/设置 点击 TabItem 时回调方法
        /// </summary>
        [Parameter]
        public Func<TabItem, Task>? OnClickTab { get; set; }

        /// <summary>
        /// 获得/设置 关闭当前 TabItem 菜单文本
        /// </summary>
        [Parameter]
        [NotNull]
        public string? CloseCurrentTabText { get; set; }

        /// <summary>
        /// 获得/设置 关闭所有 TabItem 菜单文本
        /// </summary>
        [Parameter]
        [NotNull]
        public string? CloseAllTabsText { get; set; }

        /// <summary>
        /// 获得/设置 关闭其他 TabItem 菜单文本
        /// </summary>
        [Parameter]
        [NotNull]
        public string? CloseOtherTabsText { get; set; }

        [Inject]
        [NotNull]
        private IStringLocalizer<Tab>? Localizer { get; set; }

        [Inject]
        [NotNull]
        private NavigationManager? Navigator { get; set; }

        [Inject]
        [NotNull]
        private TabItemTextOptions? Options { get; set; }

        /// <summary>
        /// OnInitializedAsync 方法
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            if (ShowExtendButtons) IsBorderCard = true;

            CloseOtherTabsText ??= Localizer[nameof(CloseOtherTabsText)];
            CloseAllTabsText ??= Localizer[nameof(CloseAllTabsText)];
            CloseCurrentTabText ??= Localizer[nameof(CloseCurrentTabText)];

            if (ClickTabToNavigation)
            {
                InitRouteTable();

                AddTabByUrl();

                Navigator.LocationChanged += Navigator_LocationChanged;
            }
        }

        private void Navigator_LocationChanged(object? sender, LocationChangedEventArgs e)
        {
            AddTabByUrl();

            StateHasChanged();
        }

        private void AddTabByUrl()
        {
            var requestUrl = Navigator.ToBaseRelativePath(Navigator.Uri);

            var tab = Items.FirstOrDefault(tab => tab.Url?.Equals(requestUrl, StringComparison.OrdinalIgnoreCase) ?? false);
            if (tab != null)
            {
                ActiveTabItem(tab);
            }
            else
            {
                AddTabItem(requestUrl);
            }
        }

        private void InitRouteTable()
        {
            var apps = AdditionalAssemblies == null ? new[] { Assembly.GetEntryAssembly() } : new[] { Assembly.GetEntryAssembly() }.Concat(AdditionalAssemblies).Distinct();
            var componentTypes = apps.SelectMany(a => a?.ExportedTypes.Where(t => typeof(IComponent).IsAssignableFrom(t)) ?? Array.Empty<Type>());

            foreach (var componentType in componentTypes)
            {
                var routeAttributes = componentType.GetCustomAttributes<RouteAttribute>(false);
                foreach (var template in routeAttributes.Select(t => t.Template))
                {
                    RouteTable.TryAdd(template.Trim('/').ToLowerInvariant(), componentType);
                }
            }
        }

        /// <summary>
        /// OnAfterRender 方法
        /// </summary>
        /// <param name="firstRender"></param>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                FirstRender = false;
            }

            await JSRuntime.InvokeVoidAsync(TabElement, "bb_tab");
        }

        private bool ShouldShowExtendButtons() => ShowExtendButtons && (Placement == Placement.Top || Placement == Placement.Bottom);

        /// <summary>
        /// 点击 TabItem 时回调此方法
        /// </summary>
        private async Task OnClickTabItem(TabItem item)
        {
            Items.ToList().ForEach(i => i.SetActive(false));
            if (OnClickTab != null) await OnClickTab(item);
            if (!ClickTabToNavigation)
            {
                item.SetActive(true);
                StateHasChanged();
            }
        }

        /// <summary>
        /// 切换到上一个标签方法
        /// </summary>
        public Task ClickPrevTab()
        {
            var item = Items.FirstOrDefault(i => i.IsActive);
            if (item != null)
            {
                var index = _items.IndexOf(item);
                if (index > -1)
                {
                    index--;
                    if (index < 0) index = _items.Count - 1;
                    if (!ClickTabToNavigation) item.SetActive(false);

                    item = Items.ElementAt(index);
                    if (ClickTabToNavigation)
                    {
                        Navigator.NavigateTo(item.Url!);
                    }
                    else
                    {
                        item.SetActive(true);
                        StateHasChanged();
                    }
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 切换到下一个标签方法
        /// </summary>
        public Task ClickNextTab()
        {
            var item = Items.FirstOrDefault(i => i.IsActive);
            if (item != null)
            {
                var index = _items.IndexOf(item);
                if (index < _items.Count)
                {
                    if (!ClickTabToNavigation) item.SetActive(false);

                    index++;
                    if (index + 1 > _items.Count) index = 0;
                    item = Items.ElementAt(index);

                    if (ClickTabToNavigation)
                    {
                        Navigator.NavigateTo(item.Url!);
                    }
                    else
                    {
                        item.SetActive(true);
                        StateHasChanged();
                    }
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 关闭当前标签页方法
        /// </summary>
        public Task CloseCurrentTab()
        {
            var tab = _items.FirstOrDefault(t => t.IsActive);
            if (tab != null && tab.Closable)
            {
                RemoveTab(tab);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 关闭所有标签页方法
        /// </summary>
        public void CloseAllTabs()
        {
            _items.RemoveAll(t => t.Closable);
        }

        /// <summary>
        /// 关闭其他标签页方法
        /// </summary>
        public void CloseOtherTabs()
        {
            _items.RemoveAll(t => t.Closable && !t.IsActive);
        }

        /// <summary>
        /// 添加 TabItem 方法 由 TabItem 方法加载时调用
        /// </summary>
        /// <param name="item">TabItemBase 实例</param>
        internal void AddItem(TabItem item) => _items.Add(item);

        /// <summary>
        /// 通过 Url 添加 TabItem 标签方法
        /// </summary>
        /// <param name="url"></param>
        /// <param name="text"></param>
        /// <param name="icon"></param>
        /// <param name="active"></param>
        /// <param name="closable"></param>
        public void AddTab(string url, string text, string? icon = null, bool active = true, bool closable = true)
        {
            AddTabItem(url, text, icon, active, closable);

            StateHasChanged();
        }

        private void AddTabItem(string url, string? text = null, string? icon = null, bool active = true, bool closable = true)
        {
            url = url.TrimStart('/').ToLowerInvariant();
            if (RouteTable.TryGetValue(url, out var comp))
            {
                AddTabItem(new Dictionary<string, object>
                {
                    [nameof(TabItem.Text)] = text ?? Options.Text ?? string.Empty,
                    [nameof(TabItem.Url)] = url,
                    [nameof(TabItem.Icon)] = icon ?? Options.Icon ?? string.Empty,
                    [nameof(TabItem.Closable)] = closable,
                    [nameof(TabItem.IsActive)] = active,
                    [nameof(TabItem.ChildContent)] = new RenderFragment(builder =>
                    {
                        builder.OpenComponent(0, comp);
                        builder.SetKey(url);
                        builder.CloseComponent();
                    })
                });
            }
        }

        /// <summary>
        /// 添加 TabItem 方法
        /// </summary>
        /// <param name="parameters"></param>
        public void AddTab(Dictionary<string, object> parameters)
        {
            AddTabItem(parameters);
            StateHasChanged();
        }

        private void AddTabItem(Dictionary<string, object> parameters)
        {
            var item = TabItem.Create(parameters);
            if (item.IsActive) _items.ForEach(i => i.SetActive(false));
            _items.Add(item);
        }

        /// <summary>
        /// 移除 TabItem 方法
        /// </summary>
        /// <param name="item"></param>
        public void RemoveTab(TabItem item)
        {
            var index = _items.IndexOf(item);
            _items.Remove(item);
            var activeItem = _items.FirstOrDefault(i => i.IsActive);
            if (activeItem == null)
            {
                // 删除的 TabItem 是当前 Tab
                if (index < _items.Count)
                {
                    // 查找后面的 Tab
                    activeItem = _items[index];
                }
                else
                {
                    // 查找前面的 Tab
                    activeItem = _items.LastOrDefault();
                }
                if (activeItem != null)
                {
                    if (ClickTabToNavigation)
                    {
                        Navigator.NavigateTo(activeItem.Url!);
                    }
                    else
                    {
                        activeItem.SetActive(true);
                        StateHasChanged();
                    }
                }
                else
                {
                    // 无标签
                    StateHasChanged();
                }
            }
        }

        /// <summary>
        /// 设置指定 TabItem 为激活状态
        /// </summary>
        /// <param name="item"></param>
        public void ActiveTab(TabItem item)
        {
            ActiveTabItem(item);

            StateHasChanged();
        }

        private void ActiveTabItem(TabItem item)
        {
            _items.ForEach(i => i.SetActive(false));
            item.SetActive(true);
        }

        /// <summary>
        /// Dispose 方法
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            Navigator.LocationChanged -= Navigator_LocationChanged;
            base.Dispose(disposing);
        }
    }
}
