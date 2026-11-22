namespace Jotter.Core

open System
open Jotter.Core.Config

type BuildContext =
    { paths: SitePaths
      config: ConfigurationModel
      theme: ThemeSource
      builtAt: DateTimeOffset }

module BuildContext =
    let create (paths: SitePaths) =
        let config = Config.load paths
        { paths = paths
          config = config
          theme = Theme.resolveProject paths config.theme
          builtAt = DateTimeOffset.UtcNow }

