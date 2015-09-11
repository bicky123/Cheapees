/*******************************************************************************
 * Copyright 2009-2015 Amazon Services. All Rights Reserved.
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 *
 * You may not use this file except in compliance with the License. 
 * You may obtain a copy of the License at: http://aws.amazon.com/apache2.0
 * This file is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
 * CONDITIONS OF ANY KIND, either express or implied. See the License for the 
 * specific language governing permissions and limitations under the License.
 *******************************************************************************
 * Void Transport Request Response
 * API Version: 2010-10-01
 * Library Version: 2015-07-01
 * Generated: Mon Jul 06 15:35:23 GMT 2015
 */


using System;
using System.Xml;
using System.Xml.Serialization;
using MWSClientCsRuntime;

namespace FBAInboundServiceMWS.Model
{
    [XmlTypeAttribute(Namespace = "http://mws.amazonaws.com/FulfillmentInboundShipment/2010-10-01/")]
    [XmlRootAttribute(Namespace = "http://mws.amazonaws.com/FulfillmentInboundShipment/2010-10-01/", IsNullable = false)]
    public class VoidTransportRequestResponse : AbstractMwsObject, IMWSResponse
    {

        private VoidTransportRequestResult _voidTransportRequestResult;
        private ResponseMetadata _responseMetadata;
        private ResponseHeaderMetadata _responseHeaderMetadata;

        /// <summary>
        /// Gets and sets the VoidTransportRequestResult property.
        /// </summary>
        [XmlElementAttribute(ElementName = "VoidTransportRequestResult")]
        public VoidTransportRequestResult VoidTransportRequestResult
        {
            get { return this._voidTransportRequestResult; }
            set { this._voidTransportRequestResult = value; }
        }

        /// <summary>
        /// Sets the VoidTransportRequestResult property.
        /// </summary>
        /// <param name="voidTransportRequestResult">VoidTransportRequestResult property.</param>
        /// <returns>this instance.</returns>
        public VoidTransportRequestResponse WithVoidTransportRequestResult(VoidTransportRequestResult voidTransportRequestResult)
        {
            this._voidTransportRequestResult = voidTransportRequestResult;
            return this;
        }

        /// <summary>
        /// Checks if VoidTransportRequestResult property is set.
        /// </summary>
        /// <returns>true if VoidTransportRequestResult property is set.</returns>
        public bool IsSetVoidTransportRequestResult()
        {
            return this._voidTransportRequestResult != null;
        }

        /// <summary>
        /// Gets and sets the ResponseMetadata property.
        /// </summary>
        [XmlElementAttribute(ElementName = "ResponseMetadata")]
        public ResponseMetadata ResponseMetadata
        {
            get { return this._responseMetadata; }
            set { this._responseMetadata = value; }
        }

        /// <summary>
        /// Sets the ResponseMetadata property.
        /// </summary>
        /// <param name="responseMetadata">ResponseMetadata property.</param>
        /// <returns>this instance.</returns>
        public VoidTransportRequestResponse WithResponseMetadata(ResponseMetadata responseMetadata)
        {
            this._responseMetadata = responseMetadata;
            return this;
        }

        /// <summary>
        /// Checks if ResponseMetadata property is set.
        /// </summary>
        /// <returns>true if ResponseMetadata property is set.</returns>
        public bool IsSetResponseMetadata()
        {
            return this._responseMetadata != null;
        }

        /// <summary>
        /// Gets and sets the ResponseHeaderMetadata property.
        /// </summary>
        [XmlElementAttribute(ElementName = "ResponseHeaderMetadata")]
        public ResponseHeaderMetadata ResponseHeaderMetadata
        {
            get { return this._responseHeaderMetadata; }
            set { this._responseHeaderMetadata = value; }
        }

        /// <summary>
        /// Sets the ResponseHeaderMetadata property.
        /// </summary>
        /// <param name="responseHeaderMetadata">ResponseHeaderMetadata property.</param>
        /// <returns>this instance.</returns>
        public VoidTransportRequestResponse WithResponseHeaderMetadata(ResponseHeaderMetadata responseHeaderMetadata)
        {
            this._responseHeaderMetadata = responseHeaderMetadata;
            return this;
        }

        /// <summary>
        /// Checks if ResponseHeaderMetadata property is set.
        /// </summary>
        /// <returns>true if ResponseHeaderMetadata property is set.</returns>
        public bool IsSetResponseHeaderMetadata()
        {
            return this._responseHeaderMetadata != null;
        }


        public override void ReadFragmentFrom(IMwsReader reader)
        {
            _voidTransportRequestResult = reader.Read<VoidTransportRequestResult>("VoidTransportRequestResult");
            _responseMetadata = reader.Read<ResponseMetadata>("ResponseMetadata");
        }

        public override void WriteFragmentTo(IMwsWriter writer)
        {
            writer.Write("VoidTransportRequestResult", _voidTransportRequestResult);
            writer.Write("ResponseMetadata", _responseMetadata);
        }

        public override void WriteTo(IMwsWriter writer)
        {
            writer.Write("http://mws.amazonaws.com/FulfillmentInboundShipment/2010-10-01/", "VoidTransportRequestResponse", this);
        }

        public VoidTransportRequestResponse() : base()
        {
        }
    }
}
