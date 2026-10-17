namespace Jotter.Core

open Jotter.Core.IOUtils

module DisqusComments =

    let render (documentUrl: string) (disqusUrl: string) =
       let documentUrl = IOUtils.escapeForJsString documentUrl
       let disqusUrl = IOUtils.escapeForJsString disqusUrl
       $"""
        <div id="disqus_thread"></div>
			<script>
			    var disqus_config = function () {{
                    this.page.url = '{documentUrl}';
                    this.page.identifier = '{documentUrl}';
                }};

			(function() {{
                var d = document, s = d.createElement('script');
                s.src = '//{disqusUrl}/embed.js';
                s.setAttribute('data-timestamp', +new Date());
                (d.head || d.body).appendChild(s);
            }})();
			</script>
			<noscript>Please enable JavaScript to view the <a href="https://disqus.com/?ref_noscript">
                      comments powered by Disqus.</a></noscript>

        """

