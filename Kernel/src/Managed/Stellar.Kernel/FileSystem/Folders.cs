namespace Stellar.Kernel.FileSystem;

/// <summary>
/// The folders of the engine, as paths of its domains: what a game names instead of locations of the machine.
/// </summary>
/// <remarks>
/// <para><c>Folders.AppData.Roaming.Combine("saves/slot1.json")</c> is <c>user://saves/slot1.json</c>. These are names
/// only, the same in every engine instance: where each lies is up to the file system of that instance.</para>
/// <para>The module Stellar.FileSystem mounts them when the engine starts. <see cref="Assets"/> and
/// <see cref="Resources"/> are there if the application has such folders, <see cref="AppData"/> and
/// <see cref="Temp"/> if it has a name.</para>
/// </remarks>
public static class Folders
{
    /// <summary>Gets the folder of the application, read-only.</summary>
    public static FilePath App { get; } = FilePath.Root("app");

    /// <summary>Gets the assets of the application, read-only.</summary>
    public static FilePath Assets { get; } = FilePath.Root("assets");

    /// <summary>Gets the compiled resources of the application, read-only.</summary>
    public static FilePath Resources { get; } = FilePath.Root("resources");

    /// <summary>Gets the folder for files that may disappear once the application stops.</summary>
    public static FilePath Temp { get; } = FilePath.Root("temp");

    /// <summary>
    /// The folders of the application among the data of the user.
    /// </summary>
    public static class AppData
    {
        /// <summary>Gets the data that follows the user from machine to machine: settings, saves.</summary>
        public static FilePath Roaming { get; } = FilePath.Root("user");

        /// <summary>Gets the data of this machine: caches, logs.</summary>
        public static FilePath Local { get; } = FilePath.Root("local");

        /// <summary>Gets the data of this machine a process with few rights may write.</summary>
        public static FilePath LocalLow { get; } = FilePath.Root("locallow");
    }
}
