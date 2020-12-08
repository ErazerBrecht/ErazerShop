// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Collections.Generic;

namespace IdentityServerHost.Quickstart.UI
{
    public class ExternalProvider
    {
        public string DisplayName { get; set; }
        public string AuthenticationScheme { get; set; }
        public ICollection<string> Hosts { get; set; } = new List<string>();
    }
}