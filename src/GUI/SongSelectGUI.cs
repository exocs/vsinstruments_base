using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.GameContent;
using Instruments.Files;

namespace Instruments.GUI
{
	public class SongSelectGUI : GuiDialog
	{
		public override string ToggleKeyCombinationCode => null;
		event Action<string> bandNameChange;

		// TODO@exocs:
		//  This structure may be kept outside of the song select GUI and cached,
		//  e.g. for as long as an instrument is held. There is no need to rebuild
		//  the tree repeatedly, especially since it updates on changes.
		// TODO@exocs:
		//  Moving it outside will also prevent nodes from collapsing or expanding
		//  when the GUI is re-opened.
		private FileTree _fileTree;


		// Summary:
		//     List of all nodes to be shown in the tree view, only displaying directories.
		private List<FileTree.Node> _treeNodes = null;
		//
		// Summary:
		//     Currently selected node in the tree view, i.e. the node determining the content view.
		private FileTree.Node _treeSelection = null;
		//
		// Summary:
		//     List of all nodes to be shown in the tree view, only displaying directories.
		private List<FileTree.Node> _contentNodes = null;
		//
		// Summary:
		//     Currently selected node in the content view, likely a node representing a file.
		private FileTree.Node _contentSelection = null;
		//
		// Summary:
		//     Stores the currently selected text filter.
		private string _textFilter;
		//
		// Summary:
		//     Flag that is set when this tree has changed.
		private bool _hasChanged;
		//
		// Summary:
		//     Object used for locking this context when accessed from other threads.
		private object _hasChangedLock = new object();
		//
		// Summary:
		//     Convenience wrapper for all text constants of individual Gui elements for this menu.
		private struct Keys
		{
			public const string TreeList = "treeList";
			public const string TreeScrollBar = "treeScrollBar";

			public const string ContentList = "contentList";
			public const string ContentScrollBar = "contentScrollBar";

			public const string LocationText = "locationText";

			public const string SearchTextInput = "searchTextInput";
		}

		private GuiElementFlatList TreeList
		{
			get
			{
				return SingleComposer.GetFlatList(Keys.TreeList);
			}
		}
		private GuiElementScrollbar TreeScrollBar
		{
			get
			{
				return SingleComposer.GetScrollbar(Keys.TreeScrollBar);
			}
		}

		private GuiElementFlatList ContentList
		{
			get
			{
				return SingleComposer.GetFlatList(Keys.ContentList);
			}
		}
		private GuiElementScrollbar ContentListScrollBar
		{
			get
			{
				return SingleComposer.GetScrollbar(Keys.ContentScrollBar);
			}
		}

		private GuiElementDynamicText LocationText
		{
			get
			{
				return SingleComposer.GetDynamicText(Keys.LocationText);
			}
		}

		private GuiElementTextInput SearchTextInput
		{
			get
			{
				return SingleComposer.GetTextInput(Keys.SearchTextInput);
			}
		}
		//
		// Summary:
		//     Create new song selection GUI with root path at the provided directory path.
		public SongSelectGUI(ICoreClientAPI capi, string directory, Action<string> bandChange = null, string bandName = "", string title = "Song Selection")
			: base(capi)
		{
			_fileTree = new FileTree(directory);

			// TODO@exocs: Solve more pragmatically, for now this is good enough.
			_fileTree.NodeChanged += (node) =>
			{
				lock (_hasChangedLock)
				{
					_hasChanged = true;
				}
			};

			_treeNodes = new List<FileTree.Node>();
			_contentNodes = new List<FileTree.Node>();

			bandNameChange = bandChange;
			SetupDialog(title, bandName);
		}
		//
		// Summary:
		//     Dispose of allocated resources once menu is closed.
		public override void OnGuiClosed()
		{
			if (_fileTree != null)
			{
				_fileTree.Dispose();
				_fileTree = null;
			}
			base.OnGuiClosed();
		}
		//
		// Summary:
		//     Prepares and composes the dialog.
		private void SetupDialog(string title, string bandName)
		{
			// Constants
			double padding = GuiStyle.ElementToDialogPadding;
			const double topBarHeight = 40;
			const double leftPaneWidth = 250;
			const double rightPaneWidth = 500;
			const double paneHeight = 400;

			// Base dialog background
			ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
				.WithAlignment(EnumDialogArea.CenterMiddle);

			// Address may be long and so it will span the entire left column and 50% of the right column
			ElementBounds addressBarBounds = ElementBounds.Fixed(padding, padding, leftPaneWidth + 0.5f * rightPaneWidth, 32);

			// Search bar will (hopefully) in most cases suffice with few characters or words, make it only half of the remaining width
			ElementBounds searchBarBounds = ElementBounds.Fixed(0, padding, 0.5 * rightPaneWidth, 32)
				.FixedRightOf(addressBarBounds, padding);

			// Content area below the navigation bar
			ElementBounds contentBounds = ElementBounds.Fixed(
				padding,
				padding + topBarHeight + 10,
				rightPaneWidth,
				paneHeight
			);

			// Tree view left pane
			ElementBounds treePaneBounds = ElementBounds.Fixed(0, 0, leftPaneWidth, paneHeight)
				.WithFixedOffset(contentBounds.fixedX, contentBounds.fixedY);

			// Tree view scrollbar
			ElementBounds treeScrollbarBounds = ElementBounds.Fixed(
				treePaneBounds.fixedX + treePaneBounds.fixedWidth,
				treePaneBounds.fixedY,
				20,
				treePaneBounds.fixedHeight
			);

			// Content view pane
			ElementBounds contentPaneBounds = ElementBounds.Fixed(
				treeScrollbarBounds.fixedX + treeScrollbarBounds.fixedWidth + 10,
				contentBounds.fixedY,
				rightPaneWidth,
				paneHeight
			);

			// Content view scrollbar
			ElementBounds contentScrollbarBounds = ElementBounds.Fixed(
				contentPaneBounds.fixedX + contentPaneBounds.fixedWidth,
				contentPaneBounds.fixedY,
				20,
				contentPaneBounds.fixedHeight
			);

			// Background
			ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(padding);
			bgBounds.BothSizing = ElementSizing.FitToChildren;
			bgBounds.WithChildren(
				addressBarBounds, searchBarBounds,
				treePaneBounds, treeScrollbarBounds,
				contentPaneBounds, contentScrollbarBounds
			);

			// Begin composing
			SingleComposer = capi.Gui.CreateCompo("FileExplorerDialog", dialogBounds)
					.AddShadedDialogBG(bgBounds)
					.AddDialogTitleBar(title, Close)
					.BeginChildElements(bgBounds)

						// Top bar with address text and search bar
						.AddDynamicText(".", CairoFont.WhiteDetailText(), addressBarBounds, Keys.LocationText)
						.AddTextInput(searchBarBounds, FilterContent, CairoFont.WhiteSmallishText(), Keys.SearchTextInput)

						// Left panel, i.e. the tree view
						.BeginClip(treePaneBounds.ForkBoundingParent())
							.AddInset(treePaneBounds, 3)
							.AddFlatListEx(treePaneBounds, OnTreeElementLeftClick, OnTreeExpandLeftClick, Unsafe.As<List<IFlatListItem>>(_treeNodes), Keys.TreeList)
						.EndClip()
						.AddVerticalScrollbarEx((float value) =>
						{
							OnScrollBarValueChanged(value, TreeList);

						}, treeScrollbarBounds, treePaneBounds, Keys.TreeScrollBar)

						// Right pane, i.e. the content view
						.BeginClip(contentPaneBounds.ForkBoundingParent())
							.AddInset(contentPaneBounds, 3)
							.AddFlatList(contentPaneBounds, OnContentElementLeftClick, Unsafe.As<List<IFlatListItem>>(_contentNodes), Keys.ContentList)
						.EndClip()
						.AddVerticalScrollbarEx((value) =>
						{
							OnScrollBarValueChanged(value, ContentList);

						}, contentScrollbarBounds, contentPaneBounds, Keys.ContentScrollBar)

					.EndChildElements()
					.Compose();


			if (bandNameChange != null)
				UpdateBand(bandName);

			// Initial refresh to get up-to-date state.
			RefreshContent(true, true);
		}
		//
		// Summary:
		//     Callback raised when the scroll bar value for provided list has changed.
		private void OnScrollBarValueChanged(float value, GuiElementFlatList list)
		{
			list.insideBounds.fixedY = -value;
			list.insideBounds.CalcWorldBounds();
		}
		//
		// Summary:
		//     Updates the scroll bar dimensions based on content in the provided list.
		private void UpdateScrollBarSize(GuiElementFlatList list, GuiElementScrollbar scrollBar)
		{
			int rowCount = list.Elements.Count;

			double rowHeight = GuiElement.scaled(list.unscaledCellHeight + list.unscaledCellSpacing);
			double scrollTotalHeight = rowHeight * rowCount;
			double scrollVisibleHeight = list.Bounds.fixedHeight;

			scrollBar.SetHeights((float)scrollVisibleHeight, (float)scrollTotalHeight);
		}
		//
		// Summary:
		//     Selects the provided tree node in the tree view.
		//     Propagates changes to content selection as well!
		private void SelectTreeNode(FileTree.Node node)
		{
			// Clear the previous selection, if there is one, as we only want
			// to allow opening a single node from within the tree.
			if (_treeSelection != null)
			{
				_treeSelection.IsSelected = false;
			}

			// Update state of the new node and make sure to update
			// the location text, so the user is aware of what they are doing.
			if (node != null)
			{
				node.IsSelected = true;
				_treeSelection = node;
			}

			// When the user selects a tree node, their content is about
			// to be changed. Drop the.
			_textFilter = string.Empty;
			SearchTextInput.SetValue(string.Empty);

			// And make sure to apply refresh the content of the tree if necessary 
			RefreshContent(true, true);
		}
		//
		// Summary:
		//     Refreshes all content state, e.g. the lists and the dimensions of their scroll bars.
		protected void RefreshContent(bool refreshTree = true, bool refreshContent = false)
		{
			if (refreshTree)
			{
				_treeNodes.Clear();
				_fileTree.GetNodes(_treeNodes, FileTree.Filter.Directories | FileTree.Filter.ExpandedOnly);
				LocationText.SetNewText(_treeSelection != null ? _treeSelection.FullPath : "-");
				UpdateScrollBarSize(TreeList, TreeScrollBar);
			}

			// Refresh content view
			if (refreshContent)
			{
				// First determine if filter is applied - with filter applied we loosen the bound
				// of direct depth search only and search recursively instead.
				bool isFilterEnabled = !string.IsNullOrEmpty(_textFilter);

				// Clear the content and fetch all nodes from the selected tree item again:
				_contentNodes.Clear();
				FileTree.Node treeNode = _treeSelection;
				if (treeNode != null)
				{
					treeNode.GetNodes(
						_contentNodes,
						FileTree.Filter.Files,
						isFilterEnabled ? 0 : 1
						);
				}

				// After fetching the content, filter the actual nodes if the filter is enabled:
				if (isFilterEnabled)
				{
					for (int i = 0; i < _contentNodes.Count;)
					{
						FileTree.Node predicate = _contentNodes[i];
						if (predicate.Name.Contains(_textFilter, StringComparison.OrdinalIgnoreCase))
						{
							++i;
							continue;
						}

						// The predicate did not match, withdraw it from the node view.
						_contentNodes.RemoveAt(i);
					}
				}

				UpdateScrollBarSize(ContentList, ContentListScrollBar);
			}
		}
		//
		// Summary:
		//     Raised when an element in the tree view is clicked.
		private void OnTreeElementLeftClick(int index)
		{
			// TODO@exocs: This can be improved,
			// also check the Visible property and its impact on the flat list??
			FileTree.Node node = _treeNodes[index];
			SelectTreeNode(node);
		}
		//
		// Summary:
		//     Raised when an expand button is clicked for an element in the tree view.
		private void OnTreeExpandLeftClick(int index)
		{
			FileTree.Node node = _treeNodes[index];
			node.IsExpanded = !node.IsExpanded;

			RefreshContent(true, false);
		}
		//
		// Summary:
		//     Raised when an item is clicked on in the content view.
		private void OnContentElementLeftClick(int index)
		{
			FileTree.Node node = _contentNodes[index];
			capi.ShowChatMessage("Selected file: " + node.FullPath);
		}
		//
		// Summary:
		//     Applies the provided string as the text filter.
		private void FilterContent(string textFilter)
		{
			_textFilter = textFilter;
			RefreshContent(false, true);
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
		//
		// Summary:
		//     Polls for periodic changes.
		public override void OnBeforeRenderFrame3D(float deltaTime)
		{
			// Poll for changes from the file tree and update the content if necessary.
			lock (_hasChangedLock)
			{
				if (_hasChanged)
				{
					_hasChanged = false;
					RefreshContent(true, true);
				}
			}

			base.OnBeforeRenderFrame3D(deltaTime);
		}

		private void Close()
		{
			TryClose();
		}
	}
}
