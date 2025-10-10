using System;
using Vintagestory.API.Client;

namespace Instruments.GUI
{
	//
	// Summary:
	//     This class only exists as a workaround to resolve a native limitation of the GuiElementScrollbar,
	//     which only checks the vertical area of the bounds, which prevents multiple scrollbars placed on
	//     the screen simultaneously from interacting properly.
	internal class GuiElementScrollbarEx : GuiElementScrollbar
	{
		//
		// Summary:
		//     The bounds of the corresponding element that should also intercept scrolling
		//     events as part of this scrollbar. Typically an associated list of items.
		protected ElementBounds ContentBounds { get; private set; }
		//
		// Summary:
		//     Create new scrollbar gui element that only intercepts scrolling events within its bounds or
		//      bounds of its associated content element, if provided.
		public GuiElementScrollbarEx(ICoreClientAPI capi, Action<float> onNewScrollbarValue, ElementBounds bounds, ElementBounds contentBounds)
				: base(capi, onNewScrollbarValue, bounds)
		{
			ContentBounds = contentBounds;
		}
		//
		// Summary:
		//     Resolve the mouse scrolling event, making sure that the event is only consumed if the 
		//     pointer is within the bounds of this or the content element, resolving the native limitation.
		public override void OnMouseWheel(ICoreClientAPI api, MouseWheelEventArgs args)
		{
			double mouseX = api.Input.MouseX;
			double mouseY = api.Input.MouseY;

			// Check the direct bounds of the scrollbar only
			if (!Bounds.PointInside(mouseX, mouseY))
			{
				// With defined content bounds, fallback to also checking the bounds
				// of the content. If not inside, terminate the event, preventing any
				// unwated scrolling from external elements.
				if (ContentBounds != null)
				{
					ContentBounds.CalcWorldBounds();
					if (!ContentBounds.PointInside(mouseX, mouseY))
						return;
				}
				else
				{
					return;
				}
			}

			// Fallback to base implementation
			base.OnMouseWheel(api, args);
		}
	}
	//
	// Summary:
	//     This class implements GUI extensions methods similarly to follow the game convention.
	internal static class GuiExtensions
	{
		//
		// Summary:
		//     Creates new scroll bar element with optional element bounds that ensures scroll events are only intercepted
		//     while the pointer is within the scrollbar bounds or the content bounds, if provided.
		public static GuiComposer AddVerticalScrollbarEx(this GuiComposer composer, Action<float> onNewScrollbarValue, ElementBounds bounds, ElementBounds contentBound = null, string key = null)
		{
			if (!composer.Composed)
			{
				composer.AddInteractiveElement(new GuiElementScrollbarEx(composer.Api, onNewScrollbarValue, bounds, contentBound), key);
			}

			return composer;
		}
	}
}
