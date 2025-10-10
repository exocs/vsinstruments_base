using System;                   // Action<>
using System.Collections.Generic; // Lists
using System.Runtime.CompilerServices;
using Instruments.Files;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace Instruments.GUI
{
	internal static class GuiExt
	{
		// The scrollbar only ever checks Y axis.
		// This little hack exists so it also checks X axis in bounds check,
		// but also it will use provided content's bounds to "retain" the original functionality
		// TODO@exocs: Move this out to some utility GUI class
		private class GuiElementScrollbarEx : GuiElementScrollbar
		{
			private ElementBounds _contentBounds;

			public GuiElementScrollbarEx(ICoreClientAPI capi, Action<float> onNewScrollbarValue, ElementBounds bounds, ElementBounds contentBounds)
				: base(capi, onNewScrollbarValue, bounds)
			{
				_contentBounds = contentBounds;
			}

			public override void OnMouseWheel(ICoreClientAPI api, MouseWheelEventArgs args)
			{
				double mX = api.Input.MouseX, mY = api.Input.MouseY;
				if (!Bounds.PointInside(mX, mY))
				{
					if (_contentBounds != null)
					{
						// Not even in contnet!
						_contentBounds.CalcWorldBounds();
						if (!_contentBounds.PointInside(mX, mY))
							return;
					}
					else
					{
						return;
					}
				}

				base.OnMouseWheel(api, args);
			}
		}

		public static GuiComposer AddVerticalScrollbarEx(this GuiComposer composer, Action<float> onNewScrollbarValue, ElementBounds bounds, ElementBounds contentBound = null, string key = null)
		{
			if (!composer.Composed)
			{
				composer.AddInteractiveElement(new GuiElementScrollbarEx(composer.Api, onNewScrollbarValue, bounds, contentBound), key);
			}

			return composer;
		}
	}

	public class SongSelectGUI : GuiDialog
	{

		public delegate void SongSelected(FileTree.Node node);

		public override string ToggleKeyCombinationCode => null;
		event Action<string> bandNameChange;


		int listHeight = 500;
		int listWidth = 700;

		//private string filter = "";

		//private List<IFlatListItem> allStackListItems = new List<IFlatListItem>();
		//private List<IFlatListItem> shownStackListItems = new List<IFlatListItem>();

		// TODO@exocs:
		//  This structure may be kept outside of the song select GUI and cached,
		//  e.g. for as long as an instrument is held. There is no need to rebuild
		//  the tree repeatedly, especially since it updates on changes.
		private FileTree _fileTree;

		//
		// Summary:
		//     Currently selected node in the tree view, i.e. the node determining the content view.
		private FileTree.Node _treeViewSelectedNode = null;
		//
		// Summary:
		//     List of all nodes to be shown in the tree view, only displaying directories.
		private List<FileTree.Node> _treeViewNodes;
		//
		// Summary:
		//     List of all nodes to be shown in the tree view, only displaying directories.
		private List<FileTree.Node> _contentViewNodes;



		private const string TreeViewList = "treeViewList";
		private const string TreeViewListScrollBar = "treeViewScrollbar";
		private const string ContentList = "contentList";
		private const string ContentListScrollBar = "contentListScrollbar";


		public SongSelectGUI(ICoreClientAPI capi, string directory, Action<string> bandChange = null, string bandName = "") : base(capi)
		{
			_fileTree = new FileTree(directory);
			_treeViewNodes = new List<FileTree.Node>();
			_contentViewNodes = new List<FileTree.Node>();

			bandNameChange = bandChange;
			SetupDialog(bandName);
		}

		public override void OnGuiClosed()
		{
			if (_fileTree != null)
			{
				_fileTree.Dispose();
				_fileTree = null;
			}
			base.OnGuiClosed();
		}

		public override void OnGuiOpened()
		{
			FilterSongs();
			SelectTreeViewNode(_treeViewNodes.Count > 0 ? _treeViewNodes[0] : null);
			SetupScrollbars();
			//SingleComposer.GetTextInput("search").SetValue("");
			base.OnGuiOpened();
		}

		private void SetupDialog(string bandName)
		{
			// Constants
			double padding = GuiStyle.ElementToDialogPadding;
			const double topBarHeight = 40;
			const double leftPaneWidth = 200;
			const double paneHeight = 400;

			// Base dialog background
			ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
				.WithAlignment(EnumDialogArea.CenterMiddle);

			// Top navigation panel, back and up buttons, address bar and search
			ElementBounds backButtonBounds = ElementBounds.Fixed(padding, padding, 32, 32);
			ElementBounds upButtonBounds = ElementBounds.Fixed(0, padding, 32, 32)
				.FixedRightOf(backButtonBounds, 4);
			ElementBounds addressLabelBounds = ElementBounds.Fixed(0, padding, 60, 32)
				.FixedRightOf(upButtonBounds, 10);
			ElementBounds addressBarBounds = ElementBounds.Fixed(0, padding, 300, 32)
				.FixedRightOf(addressLabelBounds, 5);
			ElementBounds searchBarBounds = ElementBounds.Fixed(0, padding, 200, 32)
				.FixedRightOf(addressBarBounds, 20);

			// Content area below the navigation bar
			ElementBounds contentBounds = ElementBounds.Fixed(
				padding,
				padding + topBarHeight + 10,
				650,
				paneHeight
			);

			// Tree view left pane
			ElementBounds treePaneBounds = ElementBounds.Fixed(0, 0, leftPaneWidth, paneHeight)
				.WithFixedOffset(contentBounds.fixedX, contentBounds.fixedY);

			// Tree view scrollbar
			ElementBounds treeScrollbarBounds = ElementBounds.Fixed(
				treePaneBounds.fixedX + treePaneBounds.fixedWidth, // right at pane edge
				treePaneBounds.fixedY,                             // align top
				20,
				treePaneBounds.fixedHeight
			);

			// Content view pane
			ElementBounds contentPaneBounds = ElementBounds.Fixed(
				treeScrollbarBounds.fixedX + treeScrollbarBounds.fixedWidth + 10,
				contentBounds.fixedY,
				400,
				paneHeight
			);

			// Content view scrollbar
			ElementBounds contentScrollbarBounds = ElementBounds.Fixed(
				contentPaneBounds.fixedX + contentPaneBounds.fixedWidth,
				contentPaneBounds.fixedY,
				20,
				contentPaneBounds.fixedHeight
			);

			ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(padding);
			bgBounds.BothSizing = ElementSizing.FitToChildren;
			bgBounds.WithChildren(
				backButtonBounds, upButtonBounds,
				addressLabelBounds, addressBarBounds, searchBarBounds,
				treePaneBounds, treeScrollbarBounds,
				contentPaneBounds, contentScrollbarBounds
			);

			// TODO@exocs: Move implement, whatever
			void OnBackPressed(bool b) { }
			void OnUpPressed(bool b) { }

			// Begin composing
			SingleComposer = capi.Gui.CreateCompo("FileExplorerDialog", dialogBounds)
					.AddShadedDialogBG(bgBounds)
					.AddDialogTitleBar("File Explorer", Close)
					.BeginChildElements(bgBounds)

						// Top navigation bar
						// TODO@exocs: These don't draw correctly
						.AddIconButton("<", CairoFont.WhiteSmallText(), OnBackPressed, backButtonBounds, "backButton")
						.AddIconButton("↑", CairoFont.WhiteSmallText(), OnUpPressed, upButtonBounds, "upButton")
						.AddStaticText("Address:", CairoFont.WhiteDetailText(), EnumTextOrientation.Left, addressLabelBounds)
						.AddTextInput(addressBarBounds, (text) => { }, CairoFont.WhiteSmallishText(), "addressBar")
						.AddTextInput(searchBarBounds, (text) => { }, CairoFont.WhiteSmallishText(), "searchBar")

						// Left panel, i.e. the tree view
						.BeginClip(treePaneBounds.ForkBoundingParent())
							.AddInset(treePaneBounds, 3)
							.AddFlatList(treePaneBounds, OnTreeViewButtonPress, Unsafe.As<List<IFlatListItem>>(_treeViewNodes), TreeViewList)
						.EndClip()
						.AddVerticalScrollbarEx((float value) =>
						{
							GuiElementFlatList list = SingleComposer.GetFlatList(TreeViewList);
							OnTreeScroll(value, list);

						}, treeScrollbarBounds, treePaneBounds, TreeViewListScrollBar)

						// Right pane, i.e. the content view
						.BeginClip(contentPaneBounds.ForkBoundingParent())
							.AddInset(contentPaneBounds, 3)
							.AddFlatList(contentPaneBounds, OnContentViewButtonPress, Unsafe.As<List<IFlatListItem>>(_contentViewNodes), ContentList)
						.EndClip()
						.AddVerticalScrollbarEx((value) =>
						{
							GuiElementFlatList list = SingleComposer.GetFlatList(ContentList);
							OnTreeScroll(value, list);

						}, contentScrollbarBounds, contentPaneBounds, ContentListScrollBar)

					.EndChildElements()
					.Compose();


			if (bandNameChange != null)
				UpdateBand(bandName);

		}
		private void SetupScrollbars()
		{
			SetScrollbarSize(TreeViewList, TreeViewListScrollBar, _treeViewNodes.Count);
			SetScrollbarSize(ContentList, ContentListScrollBar, _contentViewNodes.Count);
		}

		private void OnTreeScroll(float value, GuiElementFlatList flatList)
		{
			if (flatList == null)
				return;

			flatList.insideBounds.fixedY = -value;
			flatList.insideBounds.CalcWorldBounds();
		}

		private void SetScrollbarSize(string listName, string scrollbarName, int numRows)
		{
			GuiElementFlatList stacklist = SingleComposer.GetFlatList(listName);
			GuiElementScrollbar scrollbar = SingleComposer.GetScrollbar(scrollbarName);
			if (stacklist == null || scrollbar == null)
				return;


			float rowHeight = (float)GuiElement.scaled(stacklist.unscaledCellHeight);
			float scrollTotalHeight = rowHeight * numRows;
			// TODO@exocs: Is the list height represented by a prop of the flat list anywhere?
			// also drop the stacklist naming
			float scrollVisibleHeight = (float)(this.listHeight - 6); // pane height
			scrollbar.SetHeights(scrollVisibleHeight, scrollTotalHeight);
		}

		private void OnTreeViewButtonPress(int index)
		{
			// TODO@exocs: This can be improved,
			// also check the Visible property and its impact on the flat list??
			FileTree.Node node = _treeViewNodes[index];

			SelectTreeViewNode(node);
			SelectContentViewNode(node);

			//GuiHandbookTextPage page = (GuiHandbookTextPage)shownStackListItems[index];
			//PlaySong(page.Title);
			FilterSongs();
			SetupScrollbars();
		}
		private void OnContentViewButtonPress(int index)
		{
			// TODO@exocs: This can be improved,
			// also check the Visible property and its impact on the flat list
			FileTree.Node node = _treeViewNodes[index];
			//SetupScrollbars();
		}
		
		//TODO@exocs: Demessify all of the below and unify the logic
		private void SelectTreeViewNode(FileTree.Node node)
		{
			// Make sure to check and clear previous selection, as we only
			// want to allow one item being selected (ever) in the tree view,
			// as it dictates which content to draw in the content view.
			if (_treeViewSelectedNode != null)
				_treeViewSelectedNode.IsSelected = false;

			// The selection may be fully cleared as well.
			if (node != null)
			{
				node.IsSelected = true;
			}

			_treeViewSelectedNode = node;
		}
		private void SelectContentViewNode(FileTree.Node node)
		{
			_contentViewNodes.Clear();
			if (node == null || !node.IsDirectory)
			{
				return;
			}

			node.GetNodes(_contentViewNodes, FileTree.Filter.Files, 1);
			// Update scrollbar!
		}

		private void FilterSongs()
		{
			// Update the tree view?
			_treeViewNodes.Clear();
			_fileTree.GetNodes(_treeViewNodes, FileTree.Filter.Directories);

			/*GuiElementFlatList stacklist = SingleComposer.GetFlatList("stacklist");
			stacklist.CalcTotalHeight();
			OnNewScrollbarValue();*/
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
