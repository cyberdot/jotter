namespace Jotter.Core

open System
open Jotter.Core.Config

type BuildContext =
    { paths: SitePaths
      config: ConfigurationModel
      builtAt: DateTimeOffset }

module BuildContext =
    let create (paths: SitePaths) =
        { paths = paths
          config = Config.load paths
          builtAt = DateTimeOffset.UtcNow }

