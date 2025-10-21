using ProtoBuf;

namespace Instruments.Network.Playback
{
	//
	// Summary:
	//     Response packet sent to the instigator (the requesting client) of the playback, when the playback gets denied.
	[ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
	public class StartPlaybackDenyOwner
	{
		//
		// Summary:
		//     Specifies the reasons for which a playback may be denied.
		public enum DenyReason
		{
			//
			// Summary:
			//     Unspecified, general failure response.
			Unspecified,
			//
			// Summary:
			//     Request was denied because the file was invalid.
			InvalidFile,
			//
			// Summary:
			//     Request was denied because there were too many requests.
			TooManyRequests,
			//
			// Summary:
			//     Request was denied because there is an ongoing operation already.
			OperationInProgress,
		}
		//
		// Summary:
		//     The reason why the playback was denied.
		public DenyReason Reason;
		//
		// Summary:
		//     Returns the reason for which the request was denied.
		public string ReasonText
		{
			get
			{
				switch (Reason)
				{
					case DenyReason.InvalidFile:
						return "Invalid file request.";
					case DenyReason.TooManyRequests:
						return "Too many requests.";
					case DenyReason.OperationInProgress:
						return "An operation is already in progress.";
				}

				return "Unspecified reason.";
			}
		}
	}
}