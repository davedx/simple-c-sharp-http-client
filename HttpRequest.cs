/**
MIT License

Copyright (C) 2012 David Clayton <davedx@gmail.com> www.dave78.com

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.
*/
using UnityEngine;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;

/**
 * This is a very simple, very stripped down HTTP client for use with Unity on iOS so you can still build projects with the .NET 2.0 Subset and
 * have reasonable sized apps produced.
 * It has only been tested doing very simple GET and POST requests to an Apache server.
 * Use at your own risk!
 * Pull requests welcome, but only if they do not add dependencies. (I'll add a list of what's OK at some point)
 */
public class HttpRequest 
{	
	public bool			isBusy		
	{
		get { return !succeeded && !failed; }
	}
		
	public bool			succeeded	{ get; private set; }
	public bool			failed		{ get; private set; }
	
	public Dictionary<string, string>	responseHeaders	{ get; private set; }
	private string headersRaw;
	public string						responseText	{ get; private set; }
	
	/**
	 * Create a new request with a byte[] for the request body.
	 * IT IS YOUR RESPONSIBILITY TO ENCODE THE BODY PROPERLY ACCORDING TO THE HTTP SPEC.
	 * The response from the request will be stored in responseHeaders and responseText when the request has completed.
	 * Check isBusy to find out when it's completed.
	 */
	public HttpRequest(string path, string method, byte[] bytes)
	{
		Coroutines.Run(DoRequest(path, method, bytes));
	}
	
	/**
	 * Create a new request with a string for the request body.
	 * IT IS YOUR RESPONSIBILITY TO ENCODE THE BODY PROPERLY ACCORDING TO THE HTTP SPEC.
	 * The response from the request will be stored in responseHeaders and responseText when the request has completed.
	 * Check isBusy to find out when it's completed.
	 */
	public HttpRequest(string path, string method, string body)
	{
		Coroutines.Run(DoRequest(path, method, body));
	}
	
	private IEnumerator DoRequest(string path, string method, string body)
	{
		return DoRequest(path, method, System.Text.Encoding.UTF8.GetBytes(body));	
	}
	
	private IEnumerator DoRequest(string path, string method, byte[] bodyBytes)
	{
		TcpClient tcp = new TcpClient("stickystudiosprojects.com", 80);
		NetworkStream stream = tcp.GetStream();
		
		string request 		= method+" "+path+" HTTP/1.0\r\n";
		request += "Content-Type: application/x-www-form-urlencoded; charset=utf-8\r\n";
		request += "Content-Length: " + bodyBytes.Length + "\r\n";
		request += "\r\n";
		
		byte[] requestBytes = System.Text.Encoding.UTF8.GetBytes(request);
		
		stream.Write(requestBytes, 0, requestBytes.Length);
		stream.Write(bodyBytes, 0, bodyBytes.Length);
		
		headersRaw = "";
		yield return Coroutines.Run(ReadHeaders(stream));
		
		responseHeaders = ParseHeaders(headersRaw);
		if(!responseHeaders.ContainsKey("Content-Length"))
			throw new System.Exception("Invalid server response");
		
		int contentLength = int.Parse(responseHeaders["Content-Length"]);
		
		responseText = "";
		yield return Coroutines.Run(ReadBody(stream, contentLength));
		succeeded		= true;
	}
	
	private IEnumerator ReadHeaders(NetworkStream stream)
	{
		int i = 0;
		while(i < 500)
		{
			if(stream.DataAvailable)
			{
				byte[] buff = new byte[1];
				int read = stream.Read(buff, 0, 1);
				headersRaw += System.Text.Encoding.UTF8.GetString(buff, 0, read);
				if(headersRaw.EndsWith("\r\n\r\n"))
					yield break;
				i = 0;
			}
			else
			{
				i++; 
				yield return new WaitForSeconds(0.01f);
			}
		}
		throw new System.IO.IOException("Network read timed out");
	}	

	private IEnumerator ReadBody(NetworkStream stream, int contentLength)
	{
		int i = 0;
		int bytesRead = 0;
		while(i < 5000)
		{
			if(stream.DataAvailable)
			{
				byte[] buff = new byte[1024];
				int read = stream.Read(buff, 0, buff.Length);
				responseText += System.Text.Encoding.UTF8.GetString(buff, 0, read);
				bytesRead += read;
				
				if(bytesRead >= contentLength)
					yield break;
				i = 0;
			}
			else
			{
				i++; 
				yield return new WaitForSeconds(0.001f);
			}
		}
		throw new System.IO.IOException("Network read timed out");
	}	
	
	private Dictionary<string,string> ParseHeaders(string headers)
	{
		Dictionary<string,string> dict = new Dictionary<string, string>();
		string[] lines = headers.Split ("\n".ToCharArray());
		foreach(string line in lines)
		{
			if(line.Contains (":"))
			{
				string[] parts = line.Split (":".ToCharArray());
				dict.Add (parts[0], parts[1].Trim());
			}
		}
		return dict;
	}
}
