// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ApplicationInfo.OktaVerifyInformation
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Devices.SDK;
using SQLite;
using System.Runtime.Serialization;

namespace Okta.Authenticator.NativeApp.ApplicationInfo
{
  [DataContract]
  internal class OktaVerifyInformation : IOktaVerifyInformation
  {
    public OktaVerifyInformation()
    {
    }

    public OktaVerifyInformation(string id, SandboxIntegrityState state)
      : this(id, (byte[]) null, (string) null, state)
    {
    }

    public OktaVerifyInformation(
      string id,
      byte[] instanceIdentifier,
      string sandboxName,
      SandboxIntegrityState state)
    {
      this.Id = id;
      this.InstanceIdentifier = instanceIdentifier;
      this.SandboxName = sandboxName;
      this.SandboxState = state;
    }

    [DataMember(Name = "id")]
    [PrimaryKey]
    public string Id { get; set; }

    [DataMember(Name = "instId")]
    public byte[] InstanceIdentifier { get; set; }

    [DataMember(Name = "sndbx")]
    public string SandboxName { get; set; }

    [DataMember(Name = "sndbxErr")]
    public SandboxIntegrityState SandboxState { get; set; }
  }
}
