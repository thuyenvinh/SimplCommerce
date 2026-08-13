namespace SimplCommerce.Module.Shipments.Models
{
    // G01: shipment lifecycle state machine. OrderStatus.Shipping/Shipped tracks
    // the order-level view; this enum tracks the individual shipment so an order
    // with multiple shipments can be partially delivered. Values are gapped to
    // leave room for intermediate states (Picking, AtCarrier, OutForDelivery).
    public enum ShipmentStatus
    {
        Pending = 1,
        Shipped = 20,
        Delivered = 40,
        Returned = 60,
        Cancelled = 80
    }
}
