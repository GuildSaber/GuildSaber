using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CP_SDK.UI.Components;
using CP_SDK.XUI;
using GuildSaber.Mod.Helpers;
using TMPro;
using UnityEngine;

namespace GuildSaber.Mod.Features.PlayerCard.UI.Components;

internal sealed class PlayerCardBalance : XUIVLayout
{
    private XUIText _dots = null!;
    private XUIText _value = null!;

    private PlayerCardBalance() : base("PlayerCardBalance")
    {
        SetPadding(0);
        SetSpacing(-1.5f);
        OnReady(Build);
    }

    public static PlayerCardBalance Make() => new();

    public void Render(double equilibrium, Color accent)
    {
        var filledDots = Mathf.Clamp((int)Math.Round(equilibrium / 20), 0, 5);
        var filledColor = ColorUtility.ToHtmlStringRGBA(accent);
        var emptyColor = ColorUtility.ToHtmlStringRGBA(new Color(1, 1, 1, 0.25f));
        var dots = string.Join(" ", Enumerable.Range(0, 5)
            .Select(x => $"<color=#{(x < filledDots ? filledColor : emptyColor)}>●</color>"));

        _dots.SetText(dots);
        _value.SetText($"({equilibrium:0.##}%)").SetColor(accent);
    }

    private void Build(CHOrVLayout layout)
    {
        layout.LElement.flexibleHeight = 0;
        layout.HOrVLayoutGroup.childForceExpandHeight = false;
        layout.HOrVLayoutGroup.childForceExpandWidth = false;
        layout.HOrVLayoutGroup.childAlignment = TextAnchor.MiddleCenter;

        XUIText.Make(string.Empty)
            .Bind(ref _dots)
            .SetFontSize(3.2f)
            .SetAlign(TextAlignmentOptions.Midline)
            .SetWrapping(false)
            .BuildUI(layout.transform);
        XUIText.Make(string.Empty)
            .Bind(ref _value)
            .SetFontSize(3.2f)
            .SetAlign(TextAlignmentOptions.Midline)
            .SetWrapping(false)
            .BuildUI(layout.transform);
    }
}

internal sealed class PlayerCardCategories : XUIVLayout
{
    private const int CategoriesPerPage = 6;
    private readonly List<CategoryCell> _categoryCells = [];
    private XUIText _averageLevel = null!;
    private PlayerCardBalance _balance = null!;
    private ImmutableArray<PlayerCardCategoryLevel> _categories = [];
    private Color _categoryAccent;
    private int _page;
    private XUISecondaryButton _pageLeft = null!;
    private XUIHLayout _pager = null!;
    private XUISecondaryButton _pageRight = null!;

    private PlayerCardCategories() : base("PlayerCardCategories")
    {
        SetPadding(0);
        SetSpacing(0);
        OnReady(Build);
    }

    private sealed class CategoryCell : XUIHLayout
    {
        private XUIText _level = null!;
        private XUIText _name = null!;

        public CategoryCell() : base("PlayerCardCategory")
        {
            SetWidth(30);
            SetPadding(0);
            SetSpacing(0.6f);
            OnReady(Build);
        }

        public void Render(PlayerCardCategoryLevel? category)
        {
            if (category is not { } value)
            {
                _name.SetText(string.Empty);
                _level.SetText(string.Empty);
                return;
            }

            _name.SetText($"{value.CategoryName}:");
            _level.SetText(value.LevelName).SetColor(value.Color);
        }

        private void Build(CHOrVLayout layout)
        {
            layout.HOrVLayoutGroup.childForceExpandHeight = false;
            layout.HOrVLayoutGroup.childForceExpandWidth = false;
            layout.HOrVLayoutGroup.childAlignment = TextAnchor.MiddleCenter;

            XUIText.Make(string.Empty)
                .Bind(ref _name)
                .SetFontSize(3.6f)
                .SetAlign(TextAlignmentOptions.MidlineRight)
                .SetWrapping(false)
                .BuildUI(layout.transform);
            XUIText.Make(string.Empty)
                .Bind(ref _level)
                .SetFontSize(3.6f)
                .SetStyle(FontStyles.Bold)
                .SetAlign(TextAlignmentOptions.MidlineLeft)
                .SetWrapping(false)
                .BuildUI(layout.transform);
        }
    }

    public static PlayerCardCategories Make() => new();

    public void Render(PlayerCardProgress progress, Color accent)
    {
        _categories = progress.Categories;
        _categoryAccent = accent;
        _averageLevel
            .SetText($"Avg {progress.AverageCategoryLevel:0.##}")
            .SetColor(progress.AverageCategoryLevelColor);
        _balance.Render(progress.Equilibrium, accent);
        RenderPage();
    }

    private void Build(CHOrVLayout layout)
    {
        layout.LElement.flexibleHeight = 0;
        layout.HOrVLayoutGroup.childForceExpandHeight = false;
        layout.HOrVLayoutGroup.childAlignment = TextAnchor.MiddleCenter;

        _balance = PlayerCardBalance.Make();
        XUIHLayout.Make(
                MakeCategoryColumn(),
                XUIVLayout.Make(
                        XUIText.Make(string.Empty)
                            .Bind(ref _averageLevel)
                            .SetFontSize(3.8f)
                            .SetStyle(FontStyles.Bold | FontStyles.Italic)
                            .SetWrapping(false),
                        _balance)
                    .OnReady(x =>
                    {
                        x.LElement.flexibleHeight = 0;
                        x.HOrVLayoutGroup.childForceExpandHeight = false;
                        x.HOrVLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
                    })
                    .SetWidth(24)
                    .SetPadding(0)
                    .SetSpacing(-1f),
                MakeCategoryColumn())
            .OnReady(x =>
            {
                x.LElement.flexibleHeight = 0;
                x.HOrVLayoutGroup.childForceExpandHeight = false;
                x.HOrVLayoutGroup.childForceExpandWidth = false;
                x.HOrVLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            })
            .SetWidth(86)
            .SetHeight(16.5f)
            .SetPadding(0)
            .SetSpacing(1)
            .BuildUI(layout.transform);

        XUIHLayout.Make(
                XUISecondaryButton.Make("<", PageLeft)
                    .Bind(ref _pageLeft).SetColor(Color.white).SetWidth(5).SetHeight(3),
                XUISecondaryButton.Make(">", PageRight)
                    .Bind(ref _pageRight).SetColor(Color.white).SetWidth(5).SetHeight(3))
            .Bind(ref _pager)
            .OnReady(x => x.LElement.flexibleHeight = 0)
            .SetHeight(3)
            .SetPadding(0)
            .SetSpacing(16)
            .BuildUI(layout.transform);
    }

    private XUIVLayout MakeCategoryColumn()
        => XUIVLayout.Make()
            .OnReady(column =>
            {
                column.LElement.flexibleHeight = 0;
                column.HOrVLayoutGroup.childForceExpandHeight = false;
                column.HOrVLayoutGroup.childAlignment = TextAnchor.MiddleCenter;

                for (var i = 0; i < CategoriesPerPage / 2; i++)
                {
                    var category = new CategoryCell();
                    category.BuildUI(column.transform);
                    _categoryCells.Add(category);
                }
            })
            .SetPadding(0)
            .SetSpacing(-1f);

    private void RenderPage()
    {
        var lastPage = _categories.IsEmpty ? 0 : (_categories.Length - 1) / CategoriesPerPage;
        _page = Mathf.Clamp(_page, 0, lastPage);
        var categories = _categories
            .Skip(_page * CategoriesPerPage)
            .Take(CategoriesPerPage)
            .ToArray();
        ReadOnlySpan<int> displayOrder = [0, 2, 4, 1, 3, 5];

        for (var i = 0; i < _categoryCells.Count; i++)
        {
            var categoryIndex = displayOrder[i];
            _categoryCells[i].Render(categoryIndex < categories.Length ? categories[categoryIndex] : null);
        }

        _pager.SetActive(lastPage > 0);
        SetPageButton(_pageLeft, _page > 0);
        SetPageButton(_pageRight, _page < lastPage);
    }

    private void PageLeft()
    {
        _page--;
        RenderPage();
    }

    private void PageRight()
    {
        _page++;
        RenderPage();
    }

    private void SetPageButton(XUISecondaryButton button, bool available)
        => button
            .SetColor(available ? _categoryAccent : Color.white.WithAlpha(0.25f))
            .SetInteractable(available);
}