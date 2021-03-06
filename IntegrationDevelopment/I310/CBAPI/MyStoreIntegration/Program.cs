﻿using System;
using IdentityModel.Client;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Net.Http;
using MyStoreIntegration.Default;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.ServiceModel;
using MyStoreIntegration.Integration;

namespace MyStoreIntegration
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            //This code is necessary only if you connect to the website
            //through the HTTPS connection and
            //you need to use custom validation of an SSL certificate
            //(for example, if the website uses a self-signed certificate).
            ServicePointManager.ServerCertificateValidationCallback += new
                RemoteCertificateValidationCallback(ValidateRemoteCertificate);
            //Discover the token endpoint
            HttpClient httpClient = new HttpClient();
            var discoveryResponse = httpClient.GetDiscoveryDocumentAsync(
                Properties.Settings.Default.IdentityEndpoint);
            //Obtain and use the access token
            var response =
                await httpClient.RequestPasswordTokenAsync(
            new PasswordTokenRequest
            {
                Address = discoveryResponse.Result.TokenEndpoint,
                ClientId = Properties.Settings.Default.ClientID,
                ClientSecret =
                    Properties.Settings.Default.ClientSecret,
                Scope = Properties.Settings.Default.Scope,
                UserName = Properties.Settings.Default.Username,
                Password = Properties.Settings.Default.Password
            });
            string accessToken = response.AccessToken;

            //Using the Default/18.200.001 endpoint
            using (DefaultSoapClient soapClient = new DefaultSoapClient())
                try
                {
                    soapClient.Endpoint.Behaviors.Add(
                    new AccessTokenAdderBehavior(accessToken));

                    //Export sales orders with multiple kinds of details
                    //PerformanceOptimization.ExportSalesOrders(soapClient);

                    //Export sales orders in batches
                    //PerformanceOptimization.ExportSalesOrdersInBatches(soapClient);

                    //Export the list of payments of a customer
                    //PerformanceOptimization.ExportPayments(soapClient);

                    //Retrieve the files attached to a stock item
                    Attachment.ExportStockItemFiles(soapClient);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine();
                    Console.WriteLine("Press Enter to continue");
                    Console.ReadLine();
                }
                finally
                {
                    //Sign out from Acumatica ERP
                    soapClient.Logout();
                }
        }
        //A callback, which is used to validate the certificate of
        //an Acumatica ERP website in an SSL conversation
        private static bool ValidateRemoteCertificate(object sender,
            X509Certificate cert, X509Chain chain, SslPolicyErrors policyErrors)
        {
            //For simplicity, this callback always returns true.
            //In a real integration application, you must check the SSL
            //certificate here.
            return true;
        }

        //A supplementary class that adds the access token
        //to each request to the service
        private class AccessTokenAdderBehavior : IEndpointBehavior,
        IClientMessageInspector
        {
            private readonly string _accessToken;
            public AccessTokenAdderBehavior(string accessToken)
            {
                _accessToken = accessToken;
            }
            void IEndpointBehavior.Validate(ServiceEndpoint endpoint) { }
            void IEndpointBehavior.AddBindingParameters(
            ServiceEndpoint endpoint,
            BindingParameterCollection bindingParameters)
            { }
            void IEndpointBehavior.ApplyDispatchBehavior(
            ServiceEndpoint endpoint,
            EndpointDispatcher endpointDispatcher)
            { }
            void IEndpointBehavior.ApplyClientBehavior(
            ServiceEndpoint endpoint, ClientRuntime clientRuntime)
            => clientRuntime.ClientMessageInspectors.Add(this);
            object IClientMessageInspector.BeforeSendRequest(
            ref Message request, IClientChannel channel)
            {
                var httpRequestMessageProperty =
                new HttpRequestMessageProperty();
                httpRequestMessageProperty.Headers.Add(
                HttpRequestHeader.Authorization, "Bearer " + _accessToken);
                request.Properties.Remove(HttpRequestMessageProperty.Name);
                request.Properties.Add(
                HttpRequestMessageProperty.Name,
                httpRequestMessageProperty);
                return null;
            }
            void IClientMessageInspector.AfterReceiveReply(
            ref Message reply, object correlationState)
            {
            }
        }
    }
}
