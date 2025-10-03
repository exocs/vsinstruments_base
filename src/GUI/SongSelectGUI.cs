using System;                   // Action<>
using System.Collections.Generic; // Lists
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace Instruments.GUI
{
    public class SongSelectGUI : GuiDialog
    {
        public override string ToggleKeyCombinationCode => null;
        event Action<string> bandNameChange;
        //Func<string, int> PlaySong;
        Vintagestory.API.Common.Func<string, int> PlaySong;

        int listHeight = 500;
        int listWidth = 700;

        private string filter = "";

        private List<IFlatListItem> allStackListItems = new List<IFlatListItem>();
        private List<IFlatListItem> shownStackListItems = new List<IFlatListItem>();

        public SongSelectGUI(ICoreClientAPI capi, Vintagestory.API.Common.Func<string, int> playSong, List<string> files, Action<string> bandChange = null, string bandName = "") : base(capi)
        {
            bandNameChange = bandChange;
            SetupDialog(files, bandName);
            PlaySong = playSong;

        }
        public override void OnGuiOpened()
        {
            FilterSongs();
            SingleComposer.GetTextInput("search").SetValue("");
            base.OnGuiOpened();
        }
        private void SetupDialog(List<string> files, string bandName)
        {
            // https://github.com/anegostudios/vsessentialsmod/blob/master/Gui/GuiDialogHandbook.cs

            ElementBounds searchFieldBounds = ElementBounds.Fixed(GuiStyle.ElementToDialogPadding - 2, 48, 300, 32);    // little bar at the top
            ElementBounds stackListBounds = ElementBounds.Fixed(0, 0, listWidth, listHeight).FixedUnder(searchFieldBounds, 32);
            ElementBounds insetBounds = stackListBounds.FlatCopy().FixedGrow(6).WithFixedOffset(-3, -3);                // Not sure lol
            ElementBounds clipBounds = stackListBounds.ForkBoundingParent();                                            // not sure
            ElementBounds scrollbarBounds = stackListBounds.CopyOffsetedSibling(3 + stackListBounds.fixedWidth + 7).WithFixedWidth(20);

            ElementBounds searchTextBounds = ElementBounds.Fixed(GuiStyle.ElementToDialogPadding - 2, 24, 256, 32); // No idea why the y has to be different here, too afraid to ask
            ElementBounds bandBoxBounds = ElementBounds.Fixed(GuiStyle.ElementToDialogPadding + 348, 68, 128, 32);
            ElementBounds bandStringNewBounds = ElementBounds.Fixed(GuiStyle.ElementToDialogPadding + 348, 45, 128, 32);
            ElementBounds bandStringBounds = ElementBounds.Fixed(GuiStyle.ElementToDialogPadding + 540, 68, 128, 128);

            // Background boundaries. Again, just make it fit it's child elements, then add the text as a child element
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(insetBounds, stackListBounds, scrollbarBounds);

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            foreach (string file in files)
            {
                GuiHandbookTextPage page = new GuiHandbookTextPage();
                page.Title = file;
                allStackListItems.Add(page);
            }

            // Lastly, create the dialog
            Action<string> ub = UpdateBand;
            SingleComposer = capi.Gui.CreateCompo("SongSelectDialog", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Song select", Close)
                .BeginChildElements(bgBounds)
                .AddTextInput(searchFieldBounds, (newText) =>
                {
                    filter = newText;
                    FilterSongs();
                },
                CairoFont.WhiteSmallishText(), "search")
                .AddStaticText("Song Filter:", CairoFont.WhiteDetailText(), EnumTextOrientation.Left, searchTextBounds)
                .BeginClip(clipBounds)
                    .AddInset(insetBounds, 3)
                    .AddFlatList(stackListBounds, ButtonPressed, shownStackListItems, "stacklist")
                //.AddInteractiveElement(new GuiElementHandbookList(capi, stackListBounds, (int index) => {ButtonPressed(index);}, shownStackListItems), "stacklist")

                .EndClip()
                .AddVerticalScrollbar((value) =>
                {
                    GuiElementFlatList stacklist = SingleComposer.GetFlatList("stacklist");
                    stacklist.insideBounds.fixedY = 3 - value;
                    stacklist.insideBounds.CalcWorldBounds();
                }, scrollbarBounds, "scrollbar")
                .EndChildElements()
            ;
            if (bandNameChange != null)
            {
                SingleComposer
                .AddDynamicText("", CairoFont.WhiteDetailText(), bandStringBounds, "Band name")
                .AddStaticText("Set Band Name:", CairoFont.WhiteDetailText(), EnumTextOrientation.Center, bandStringNewBounds)
                .AddTextInput(bandBoxBounds, ub)
                ;
            }
            SingleComposer.Compose();

            if (bandNameChange != null)
                UpdateBand(bandName);

        }

        private void OnNewScrollbarValue()
        {
            // Max val of value will depend on how many songs are in the folder
            GuiElementScrollbar scrollbar = SingleComposer.GetScrollbar("scrollbar");
            GuiElementFlatList stacklist = SingleComposer.GetFlatList("stacklist");
            scrollbar.SetHeights(
                listHeight,
                (float)stacklist.insideBounds.fixedHeight
                );
        }

        private void ButtonPressed(int index)
        {
            GuiHandbookTextPage page = (GuiHandbookTextPage)shownStackListItems[index];
            PlaySong(page.Title);
            TryClose();
        }
        private void FilterSongs()
        {
            shownStackListItems.Clear();
            foreach (GuiHandbookTextPage song in allStackListItems)
            {
                if (filter != "")
                {
                    string lowerCase = song.Title.ToLower();
                    if (!lowerCase.Contains(filter.ToLower()))
                        continue;
                }
                shownStackListItems.Add(song);
            }
            GuiElementFlatList stacklist = SingleComposer.GetFlatList("stacklist");
            stacklist.CalcTotalHeight();
            OnNewScrollbarValue();
        }
        public void UpdateBand(string bandName)
        {
            // Called when the text needs to change. Update the SingleComposer's Dynamic text field.
            string newText;
            if (bandName != "")
                newText = "Band Name: \n\"" + bandName + "\"";
            else
                newText = "No Band";
            SingleComposer.GetDynamicText("Band name").SetNewText(newText);
            bandNameChange(bandName);
        }
        private void Close()
        {
            TryClose();
        }
    }
}
