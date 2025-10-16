using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.GameContent;
using Instruments.GUI;

namespace Instruments.Files
{
	//
	// Summary:
	//     This object is a node based tree structure representing individual directories and folders.
	//     The tree is also watching for changes and automatically 
	public class FileTree : IDisposable
	{
		//
		// Summary:
		//     Flags defining static filter logic for obtaining nodes from the FileTree.
		[Flags]
		public enum Filter
		{
			Directories = 0x01,
			Files = 0x02,
			ExpandedOnly = 0x04,
			SelectedOnly = 0x08,

			All = Directories | Files | ExpandedOnly
		}
		//
		// Summary:
		//     A single node representing a file or a directory.
		public class Node : IFlatListItem, IFlatListExpandable
		{
			[Flags]
			private enum NodeFlags : byte
			{
				IsDirectory = 0x01,
				IsExpanded = 0x02,
				IsDirty = 0x04,
				IsSelected = 0x08
			}
			private List<Node> _children;
			//
			// Summary:
			//     Texture used for drawing this node in the GUI.
			private LoadedTexture Texture { get; set; }
			//
			// Summary:
			//     Absolute path to this object.
			public virtual string FullPath
			{
				get
				{
					if (Parent != null)
						return Path.Combine(Parent.FullPath, Name);

					return Name;
				}
			}
			//
			// Summary:
			//     Absolute path to this object.
			public virtual string RelativePath
			{
				get
				{
					if (Parent != null && !Parent.IsRoot)
						return Path.Combine(Parent.RelativePath, Name);

					return Name;
				}
			}
			//
			// Summary:
			//     Absolute path to the directory this object lies in.
			public string DirectoryPath
			{
				get
				{
					return Path.GetDirectoryName(FullPath);
				}
			}
			//
			// Summary:
			//     Name of this object, including its extensions.
			public string Name { get; private set; }
			//
			// Summary:
			//     Parent object or null if root object.
			public Node Parent { get; private set; }
			//
			// Summary:
			//     Number of child objects.
			public int ChildCount
			{
				get
				{
					return _children.Count;
				}
			}
			//
			// Summary:
			//     Returns whether this node is empty (has no children)
			public bool IsEmpty
			{
				get
				{
					return _children.Count == 0;
				}
			}
			//
			// Summary:
			//     Returns whether this node has any subdirectories.
			public int ChildDirectoryCount
			{
				get
				{
					int count = 0;
					for (int i = 0; i < _children.Count; ++i)
						if (_children[i].IsDirectory) ++count;

					return count;
				}
			}
			//
			// Summary:
			//     Children objects of this node.
			public IReadOnlyCollection<Node> Children
			{
				get
				{
					return _children;
				}
			}
			//
			// Summary:
			//     Children objects of this node.
			private NodeFlags Flags { get; set; }
			//
			// Summary:
			//     Returns whether provided flag is set.
			private bool HasFlag(NodeFlags flag)
			{
				return Flags.HasFlag(flag);
			}
			//
			// Summary:
			//     Sets the provided flag and returns whether value has changed.
			private bool SetFlag(NodeFlags flag)
			{
				NodeFlags previous = Flags;
				Flags |= flag;
				return Flags != previous;
			}
			//
			// Summary:
			//     Clears the provided flag and returns whether value has changed.
			private bool ClearFlag(NodeFlags flag)
			{
				NodeFlags previous = Flags;
				Flags &= ~flag;
				return Flags != previous;
			}
			//
			// Summary:
			//     Returns whether this object is a directory.
			public bool IsDirectory
			{
				get
				{
					return Flags.HasFlag(NodeFlags.IsDirectory);
				}
				private set
				{
					if (value)
						Flags |= NodeFlags.IsDirectory;
					else
						Flags &= ~NodeFlags.IsDirectory;
				}
			}
			//
			// Summary:
			//     Returns whether this node is dirty and needs to be recomposed.
			private bool IsDirty
			{
				get
				{
					return HasFlag(NodeFlags.IsDirty);
				}
				set
				{
					SetFlag(NodeFlags.IsDirty);
				}
			}
			//
			// Summary:
			//     Returns whether this node represents a real item or whether it has been invalidated.
			public bool IsValid
			{
				get
				{
					return Path.IsPathRooted(FullPath);
				}
			}
			//
			// Summary:
			//     Whether this node is expanded (in any relevant view) or not.
			public bool IsExpanded
			{
				get
				{
					return HasFlag(NodeFlags.IsExpanded);
				}
				set
				{
					if (value)
					{
						SetFlag(NodeFlags.IsExpanded);
					}
					else
					{
						ClearFlag(NodeFlags.IsExpanded);
					}

					// There are valid use cases where the user wants the node to become dirty
					// by explicitly re-setting the value. Mark as dirty regardless of state.
					SetFlag(NodeFlags.IsDirty);
				}
			}
			//
			// Summary:
			//     Whether this node is selected (in any relevant view) or not.
			public bool IsSelected
			{
				get
				{
					return HasFlag(NodeFlags.IsSelected);
				}
				set
				{
					if (value)
					{
						SetFlag(NodeFlags.IsSelected);
					}
					else
					{
						ClearFlag(NodeFlags.IsSelected);
					}

					// There are valid use cases where the user wants the node to become dirty
					// by explicitly re-setting the value. Mark as dirty regardless of state.
					SetFlag(NodeFlags.IsDirty);
				}
			}
			//
			// Summary:
			//     Optional user-provided data, null by default.
			public object Context { get; set; } = null;
			//
			// Summary:
			//     Returns the depth within the tree, starting at 0 for the root node.
			public int Depth
			{
				get
				{
					int depth = 0;

					Node parent = Parent;
					while (parent != null)
					{
						++depth;
						parent = parent.Parent;
					}

					return depth;
				}
			}
			//
			// Summary:
			//     Returns whether this node is the root node.
			public virtual bool IsRoot
			{
				get
				{
					return false;
				}
			}
			//
			// Summary:
			//     Creates new node.
			public Node(string fullPath)
			{
				Name = Path.GetFileName(fullPath);
				IsDirectory = Directory.Exists(fullPath);
				IsExpanded = IsDirectory;//start expanded if directory?
				_children = new List<Node>();
				Parent = null;
			}
			//
			// Summary:
			//     Adds a child object to this node.
			internal void AddChild(Node node)
			{
				if (node.Parent != null)
					throw new Exception("Node already has a parent set!");

				_children.Add(node);
				node.Parent = this;
			}
			//
			// Summary:
			//     Compares nodes by their name with directories being first, loose files after.
			struct NodeComparer : IComparer<Node>
			{
				public int Compare(Node x, Node y)
				{
					if (x.IsDirectory && !y.IsDirectory)
						return -1;

					if (!x.IsDirectory && y.IsDirectory)
						return 1;

					return string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
				}
			}
			//
			// Summary:
			//     Adds a child object to this node.
			internal void SortChildren()
			{
				_children.Sort(new NodeComparer());
			}
			//
			// Summary:
			//     Renames this node.
			internal void RemoveChild(Node child)
			{
				if (child.Parent != this)
					throw new Exception("Trying to remove a child from an unrelated parent!");

				_children.Remove(child);
				child.Parent = null;
			}
			//
			// Summary:
			//     Renames this node.
			internal void Rename(string newName)
			{
				Name = newName;
			}
			//
			// Summary:
			//     Returns the text representation of this node, i.e. its name.
			public override string ToString()
			{
				return Name;
			}
			//
			// Summary:
			//     Finds the first node by its path relative to this tree.
			public Node Find(string path, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
			{
				// Determine whether we are searching for a file in this depth,
				// or rather just a partial path to a file in any of its children.
				int separator = path.IndexOf(Path.DirectorySeparatorChar);

				// No separator, searching for direct files; no recursion at all
				if (separator == -1)
				{
					foreach (Node child in _children)
					{
						if (string.Compare(path, child.Name, stringComparison) == 0)
							return child;
					}

					// There are no additional paths to take, the file was simply not found.
					return null;
				}


				// With partial path, search for a directory and then continue the search.
				string partialPath = path.Substring(0, separator);

				foreach (Node child in _children)
				{
					if (string.Compare(child.Name, partialPath, stringComparison) == 0)
					{
						// Update the partial path to search in and start the search in child nodes.
						partialPath = path.Substring(separator + 1);
						return child.Find(partialPath, stringComparison);
					}
				}

				// No node found whatsoever, search has failed.
				return null;
			}
			//
			// Summary:
			//     Finds nodes by their name in direct children.
			public void FindChildren(string name, List<Node> destination, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
			{
				foreach (Node child in Children)
				{
					if (string.Compare(child.Name, name, StringComparison.OrdinalIgnoreCase) == 0)
						destination.Add(child);
				}
			}
			//
			// Summary:
			//     Returns whether this node is visibile in the flat list.
			public bool Visible
			{
				get
				{
					return true;
				}
			}
			//
			// Summary:
			//     Recomposes the GUI for this node.
			public void Recompose(ICoreClientAPI capi)
			{
				Texture?.Dispose();

				CairoFont font = CairoFont.WhiteDetailText();
				if (IsSelected)
				{
					font.Color = GuiStyle.ActiveButtonTextColor;
				}
				Texture = new TextTextureUtil(capi).GenTextTexture(Name, font);
			}
			//
			// Summary:
			//     Renders this node as a list entry.
			public void RenderListEntryTo(ICoreClientAPI capi, float dt, double x, double y, double cellWidth, double cellHeight)
			{
				if (Texture == null || IsDirty)
				{
					Recompose(capi);
					IsDirty = false;
				}

				// No arbitrary scales or shenanigans here, this item needs to be drawn directly
				// into the list cell. Use the provided dimensions to draw the image, but make sure
				// to center it. It seemed to make sense to use the half offset, but after some observations,
				// it seems that only offseting it by 25% actually centers it almost perfectly? ._.
				double yPad = (cellHeight - Texture.Height) / 4.0;
				// and add slight padding from the left, otherwise the text gets very close to the
				// boundaries of the list which is very unpleasant
				double xPad = GuiElement.scaled(5.0f);

				capi.Render.Render2DTexturePremultipliedAlpha(
				Texture.TextureId,
				x + xPad,
				y + yPad,
				Texture.Width,
				Texture.Height,
				50
				);
			}
			//
			// Summary:
			//     Disposes of all resources allocated by this node.
			public void Dispose()
			{
				if (Texture != null)
				{
					Texture.Dispose();
					Texture = null;
				}
			}
			//
			// Summary:
			//     Delete and dispose of this node and all its children.
			internal void Delete()
			{
				// Unparent the node first so it can be safely manipulated with.
				if (Parent != null)
					Parent.RemoveChild(this);

				// Now that the node is no longer parented, make a copy of the children collection,
				// as it will be manipulated and start deleting the nodes (starting with leaves).
				Node[] children = new Node[_children.Count];
				_children.CopyTo(children);

				// Recursively, to start with leaves
				foreach (Node child in children)
					child.Delete();

				// Finally dispose of the node
				Dispose();
			}

			//
			// Summary:
			//     Outputs all nodes, starting with this node to the destination list.
			// Parameters:
			//   filter: Static filter determining which kind of items to retrieve.
			//   maxDepth: Maximum search depth with 0 being unlimited search depth.
			public void GetNodes(List<Node> destination, Filter filter = Filter.All, int maxDepth = -1)
			{
				bool includeDirectory = filter.HasFlag(Filter.Directories);
				bool includeFiles = filter.HasFlag(Filter.Files);
				bool includeExpandedOnly = filter.HasFlag(Filter.ExpandedOnly);
				bool includeSelectedOnly = filter.HasFlag(Filter.SelectedOnly);

				int depth = 0;
				void appendNode(Node node, List<Node> destination)
				{
					bool selectionCheck = (!includeSelectedOnly || node.IsSelected);

					if (node.IsDirectory)
					{
						if (includeDirectory && selectionCheck)
							destination.Add(node);

						if ((!includeExpandedOnly || node.IsExpanded))
						{
							if (maxDepth > 0 && ++depth > maxDepth)
								return;

							foreach (Node child in node.Children)
								appendNode(child, destination);
						}
					}
					else if (includeFiles && selectionCheck)
					{
						destination.Add(node);
					}
				}

				appendNode(this, destination);
			}
		}
		//
		// Summary:
		//     The root node of the tree.
		public class RootNode : Node
		{
			//
			// Summary:
			//     The tree this node is the root of.
			private FileTree _tree;
			//
			// Summary:
			//     The parent path of this node.
			private string _rootPath;
			//
			// Summary:
			//     Creates new root node.
			public RootNode(FileTree tree, string fullPath)
				: base(fullPath)
			{
				_tree = tree;
				_rootPath = Path.GetDirectoryName(fullPath);
			}
			//
			// Summary:
			//     Returns the full, absolute path to this node.
			public override string FullPath
			{
				get
				{
					return Path.Combine(_rootPath, Name);
				}
			}
			//
			// Summary:
			//     Returns whether this node is the root node.
			public override bool IsRoot
			{
				get
				{
					return true;
				}
			}
			//
			// Summary:
			//     Returns the tree this node is the root of.
			public FileTree Tree
			{
				get
				{
					return _tree;
				}
			}
		}
		//
		// Summary:
		//     The root node of this tree containing all the child branches, if any.
		private RootNode _rootNode;
		//
		// Summary:
		//     File system watcher responsible for dispatching updates and changes within the
		//     tree structure, so it may stay up-to-date.
		private FileSystemWatcher _watcher;
		//
		// Summary:
		//     Search pattern used when enumerating files and directories.
		private string _searchPattern = "*";
		//
		// Summary:
		//     Recursively build all the branches for the provided node.
		private void BuildBranches(Node node)
		{
			// There may be no children if the node is not a directory.
			if (!node.IsDirectory)
			{
				return;
			}

			// Retrieve the full path from the node and begin
			// recursively building all the branches.
			string fullPath = node.FullPath;

			// Gather all the topmost entries and start building the tree.
			// Make sure that the building adds the node first, before building it
			// so its full path can be evaluated correctly!
			EnumerationOptions enumerationOptions = new EnumerationOptions() { IgnoreInaccessible = true, RecurseSubdirectories = false };
			IEnumerable<string?> entries = Directory.EnumerateFileSystemEntries(fullPath, _searchPattern, enumerationOptions);
			foreach (string? entry in entries)
			{
				Node branch = new Node(entry);
				node.AddChild(branch);
				BuildBranches(branch);
			}

			// Sort all children of the branch.
			node.SortChildren();
		}
		//
		// Summary:
		//     Build the tree starting at the root path to which the entire tree is considered relative.
		private RootNode BuildTree(string rootFullPath)
		{
			RootNode root = new RootNode(this, rootFullPath);
			BuildBranches(root);
			return root;
		}
		//
		// Summary:
		//     Delete and dispose of the provided node, withdrawing it from its parent and deleting all children in the process.
		private void DeleteNode(Node node)
		{
			node.Delete();
		}
		//
		// Summary:
		//     Creates new file tree for the provided directory.
		public FileTree(string root)
		{
			if (!Path.Exists(root))
			{
				throw new ArgumentException("File tree creation failed, provided path doesn't exist!");
			}

			// Create the root node and build the entirety of the tree
			_rootNode = BuildTree(root);

			// Setup the file watcher so the tree can update if necessary
			_watcher = new FileSystemWatcher(root);

			_watcher.Changed += OnWatcherChangedEvent;
			_watcher.Created += OnWatcherCreatedEvent;
			_watcher.Renamed += OnWatcherRenamedEvent;
			_watcher.Deleted += OnWatcherDeletedEvent;

			_watcher.NotifyFilter = NotifyFilters.DirectoryName
							   | NotifyFilters.FileName
							   | NotifyFilters.LastWrite
							   | NotifyFilters.Size;
			_watcher.IncludeSubdirectories = true;
			_watcher.EnableRaisingEvents = true;
		}
		//
		// Summary:
		//     Callback raised when the watcher detects an entry was renamed.
		private void OnWatcherRenamedEvent(object sender, RenamedEventArgs args)
		{
			Node node = Find(args.OldFullPath);
			if (node != null && args.Name != null)
			{
				string newName = Path.GetFileName(args.Name);
				string oldName = node.Name;

				node.Rename(newName);
				NodeRenamed?.Invoke(node, oldName, newName);

				if (node.Parent != null)
				{
					// Since this node was renamed, the parent
					// should sort its children.
					node.Parent.SortChildren();
				}
			}
		}
		//
		// Summary:
		//     Callback raised when the watcher detects an entry was created.
		private void OnWatcherCreatedEvent(object sender, FileSystemEventArgs args)
		{
			string? directory = Path.GetDirectoryName(args.FullPath);
			if (directory == null)
				return;

			Node parent = Find(directory);
			if (parent != null)
			{
				Node node = new Node(args.FullPath);
				parent.AddChild(node);
				BuildBranches(node);

				NodeCreated?.Invoke(node);
				return;
			}
		}
		//
		// Summary:
		//     Callback raised when the watcher detects an entry was changed.
		private void OnWatcherChangedEvent(object sender, FileSystemEventArgs args)
		{
			// Update:
			//   Actually raise this event at all times, it makes certain usage
			//   much more convenient - a change is a change. Maybe consider adding
			//   event args as well as the user can handle it as they want.

			// This event is already handled as part of OnWatcherRenamedEvent
			//if (args.ChangeType == WatcherChangeTypes.Renamed ||
			//	args.ChangeType == WatcherChangeTypes.Created)
			//	return;
			Node node = Find(args.FullPath);
			if (node != null)
			{
				NodeChanged?.Invoke(node);
			}
		}
		//
		// Summary:
		//     Callback raised when the watcher detects an entry was deleted.
		private void OnWatcherDeletedEvent(object sender, FileSystemEventArgs args)
		{
			Node node = Find(args.FullPath);
			if (node != null && node.Parent != null)
			{
				NodeDeleted?.Invoke(node);
				node.Parent.RemoveChild(node);
			}
		}
		//
		// Summary:
		//     Returns the root node of this tree.
		public RootNode Root
		{
			get
			{
				return _rootNode;
			}
		}
		//
		// Summary:
		//     Returns whether this tree is a valid representation or whether it has been malformed (e.g. root folder deleted).
		public bool IsValid
		{
			get
			{
				return _rootNode != null
					&& Path.Exists(_rootNode.FullPath);
			}
		}

		public delegate void NodeChange(Node node);
		public delegate void NodeRename(Node node, string oldName, string newName);
		//
		// Summary:
		//     The callback raised when a node has been created.
		public NodeChange NodeCreated;
		//
		// Summary:
		//     The callback raised when a node has changed.
		public NodeChange NodeChanged;
		//
		// Summary:
		//     The callback raised when a node has renamed.
		public NodeRename NodeRenamed;
		//
		// Summary:
		//     The callback raised when a node is about to be deleted.
		public NodeChange NodeDeleted;
		//
		// Summary:
		//     Finds the first node by its path relative to this tree.
		public Node Find(string path, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
		{
			// In cases where the tree is malformed due to e.g. the root being removed,
			// make sure to early reject.
			if (!IsValid)
			{
				return null;
			}

			// In no case should our searches include trailing separators,
			// so make sure to remove it before any further modifications,
			// especially before starting the recursive traversal/find.
			path = Path.TrimEndingDirectorySeparator(path);

			// Replace all separators with the proper format.
			path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

			// Determine if path is full or relative; if the path is fully qualified and doesn't align
			// with this filemanager's root path, the find can automatically fail.
			if (Path.IsPathFullyQualified(path))
			{
				string rootPath = _rootNode.FullPath;
				// The provided absolute path must start with the root node's full path
				// to be able to traverse the hierarchy using relative paths. Make sure
				// that this is the case.
				if (!path.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
				{
					return null;
				}

				path = Path.GetRelativePath(rootPath, path);

			}

			// Handle the very specific case of when in case of a relative path the root path is provided.
			if (path == ".")
			{
				return _rootNode;
			}

			// With the path being relative, all searches can be done relative to the file manager root.
			return _rootNode.Find(path, stringComparison);
		}
		//
		// Summary:
		//     Outputs all nodes, starting with this node to the destination list.
		// Parameters:
		//   filter: Static filter determining which kind of items to retrieve.
		//   maxDepth: Maximum search depth with 0 being unlimited search depth.
		public void GetNodes(List<Node> destination, Filter filter = Filter.All, int maxDepth = 0)
		{
			_rootNode.GetNodes(destination, filter, maxDepth);
		}
		//
		// Summary:
		//     Dispose of this tree and of all its allocated resources.
		public void Dispose()
		{
			if (_watcher != null)
			{
				_watcher.Dispose();
				_watcher = null;
			}

			if (_rootNode != null)
			{
				DeleteNode(_rootNode);
			}
		}
	}
}
