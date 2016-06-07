using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ProsperOnline.ServiceMethods.Kiosk
{
	#region Machine Info
	[Api ("VMC request to get their detail info")]
	[Route ("/Kiosk_Info", "POST")]
	public class Kiosk_Info : IReturn<Kiosk_InfoResponse>
	{
	}

	public class Kiosk_InfoResponse
	{
		public string Name { get; set; }

		public double Location_Lat { get; set; }

		public double Location_Lon { get; set; }

		public string Location_Address { get; set; }

		public string Location_City { get; set; }

		public string Location_State { get; set; }

		public string Location_Country { get; set; }

		public string Location_ZipCode { get; set; }

		public string Picture { get; set; }

		public string Phone { get; set; }

		public ResponseStatus Status { get; set; }

		public Kiosk_InfoResponse ()
		{
			Status = new ResponseStatus () { ErrorCode = "" };
		}
	}
	#endregion
	#region Machine Media
	[Api ("VMC to get advertising media")]
	[Route ("/Kiosk_Media", "POST")]
	public class Kiosk_Media : IReturn<Kiosk_MediaResponse>
	{
	}

	public class Kiosk_MediaResponse
	{
		public List<Kiosk_MediaItemResponse> Media { get; set; }

		public ResponseStatus Status { get; set; }

		public Kiosk_MediaResponse ()
		{
			Media = new List<Kiosk_MediaItemResponse> ();
			Status = new ResponseStatus () { ErrorCode = "" };
		}
	}

	public class Kiosk_MediaItemResponse
	{
		public int Id{ get; set; }

		public string Name { get; set; }

		/// <summary>
		/// =0: Image
		/// =1: Video. Audio will be stoped when playing video
		/// =2: Play Audio Mp3
		/// =3: Stop Audio Mp3
		/// </summary>
		public int MediaType { get; set; }

		public string Filepath { get; set; }

        /// <summary>
        /// md5 hash of file path field
        /// </summary>
		public string CRCHash{ get; set; }

		public bool Status { get; set; }

		public int OrderIndex { get; set; }

		public string Note { get; set; }
		// for transition (effect)
		/// <summary>
		/// =0: Normal, no effect
		/// =1: Fade
		/// =2: will add more
		/// </summary>
		public int EffectType { get; set; }

		/// <summary>
		/// Waiting Duration, in seconds
		/// </summary>
		public int Duration { get; set; }

        /// <summary>
        /// You can by pass this field
        /// </summary>
		public string Thumbnail { get; set; }

		public Kiosk_MediaItemResponse ()
		{
		}
	}
	#endregion
}
