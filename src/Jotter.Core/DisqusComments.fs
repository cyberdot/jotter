namespace Jotter.Core

module DisqusComments =

    // documentUrl/disqusUrl land inside single-quoted JS string literals below. Both are
    // normally built from config.url (free-form user input), so a single quote or backslash
    // in there would otherwise terminate the literal early and corrupt the embedded script.
    let private escapeForJsString (value: string) =
        value.Replace("\\", "\\\\").Replace("'", "\\'")

    let render (documentUrl: string) (disqusUrl: string) =
       let documentUrl = escapeForJsString documentUrl
       let disqusUrl = escapeForJsString disqusUrl
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

