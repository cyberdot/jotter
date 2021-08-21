namespace Jotter.Core

module DisqusComments =
    
    let render (documentUrl: string) (disqusUrl: string) =
       $"""
        <div id=\"disqus_thread\"></div> 
			<script>   
			    var disqus_config = function () {{      
                    this.page.url = '{documentUrl}\';    
                    this.page.identifier = '{documentUrl}'; 
                }};  
                    
			(function() {{   
                var d = document, s = d.createElement('script');  
                s.src = '//{disqusUrl}/embed.js';  
                s.setAttribute('data-timestamp', +new Date());  
                (d.head || d.body).appendChild(s);  
            }})();  
			</script>  
			<noscript>Please enable JavaScript to view the <a href=\"https://disqus.com/?ref_noscript\"> 
                      comments powered by Disqus.</a></noscript> 
				
        """

