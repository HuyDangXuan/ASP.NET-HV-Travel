from __future__ import annotations


_CONTACT_SUBJECTS = (
    "Need advice for family tour options",
    "Question about payment deadline",
    "Can we move to another departure date",
    "Need airport pickup support",
    "Please send a detailed itinerary",
    "Interested in a custom group quote",
)


def build_notifications(ctx) -> None:
    users = ctx.collections["Users"]
    customers = ctx.collections["Customers"]
    bookings = ctx.collections["Bookings"]
    types = ("Order", "System", "Promotion", "Review", "Chat")
    items = []
    for index in range(ctx.counts["Notifications"]):
        kind = types[index % len(types)]
        booking = bookings[index % len(bookings)] if bookings else None
        if kind in {"Order", "Chat"} and users:
            recipient_id = users[index % len(users)]["_id"]
        elif kind == "Promotion" and customers:
            recipient_id = customers[index % len(customers)]["_id"]
        else:
            recipient_id = "ALL"
        title = {
            "Order": f"Booking {booking['bookingCode']} needs attention" if booking else "New booking update",
            "System": "Daily operations summary is ready",
            "Promotion": "Eligible guest segment opened a new voucher batch",
            "Review": "A verified trip review is waiting for moderation",
            "Chat": "A live chat conversation needs a staff reply",
        }[kind]
        message = {
            "Order": f"{booking['tourSnapshot']['name']} now shows status {booking['status']}." if booking else ctx.factory.sentence(),
            "System": ctx.factory.paragraph(1),
            "Promotion": ctx.factory.paragraph(1),
            "Review": ctx.factory.paragraph(1),
            "Chat": ctx.factory.paragraph(1),
        }[kind]
        items.append(
            {
                "_id": ctx.factory.object_id(),
                "recipientId": recipient_id,
                "type": kind,
                "title": title,
                "message": message,
                "link": f"/admin/notifications/{index + 1}",
                "isRead": index % 3 == 0,
                "createdAt": ctx.factory.iso_timestamp(days_back=30),
            }
        )
    ctx.collections["Notifications"] = items


def build_ancillary_leads(ctx) -> None:
    customers = ctx.collections["Customers"]
    staff_users = [user for user in ctx.collections["Users"] if user["role"] in {"Staff", "Manager"}]
    service_types = ("Visa", "Transfer", "Hotel", "CustomTour")
    destinations = ("Japan", "Korea", "Singapore", "Thailand", "Taiwan")
    items = []
    for index in range(ctx.counts["AncillaryLeads"]):
        has_customer = bool(customers) and index % 3 != 0
        customer = customers[index % len(customers)] if has_customer else None
        assignee = staff_users[index % len(staff_users)]["fullName"] if staff_users and index % 2 == 0 else ""
        full_name = customer["fullName"] if customer else ctx.factory.person_name()
        departure_date = ctx.factory.future_date(7, 90)
        items.append(
            {
                "_id": ctx.factory.object_id(),
                "customerId": customer["_id"] if customer else "",
                "serviceType": service_types[index % len(service_types)],
                "fullName": full_name,
                "email": customer["email"] if customer else ctx.factory.email(full_name, 3000 + index),
                "phone": customer["phoneNumber"] if customer else ctx.factory.phone(),
                "destination": destinations[index % len(destinations)],
                "departureDate": departure_date,
                "returnDate": ctx.factory.future_date(10, 120),
                "travellersCount": 1 + (index % 4),
                "budgetText": f"{ctx.factory.money(10_000_000, 80_000_000):,} VND",
                "requestNote": ctx.factory.paragraph(1),
                "status": ("New", "Qualified", "Quoted", "Closed")[index % 4],
                "quoteStatus": ("Open", "Sent", "Accepted", "Expired")[index % 4],
                "assignedTo": assignee,
                "source": ("Website", "Chat", "Phone")[index % 3],
                "slaDueAt": ctx.factory.future_date(1, 3),
                "createdAt": ctx.factory.iso_timestamp(days_back=25),
                "updatedAt": ctx.factory.iso_timestamp(days_back=3),
            }
        )
    ctx.collections["AncillaryLeads"] = items


def build_chat_conversations(ctx) -> None:
    staff_users = [user for user in ctx.collections["Users"] if user["role"] != "Client"]
    customers = ctx.collections["Customers"]
    items = []
    for index in range(ctx.counts["chatConversations"]):
        participant_type = "guest" if index % 2 == 0 else "customer"
        customer = customers[index % len(customers)] if participant_type == "customer" and customers else None
        assigned_user = staff_users[index % len(staff_users)] if staff_users else None
        guest_name = customer["fullName"] if customer else ctx.factory.person_name()
        items.append(
            {
                "_id": ctx.factory.object_id(),
                "conversationCode": f"CHAT-{index + 1:05d}",
                "channel": "web",
                "status": ("open", "open", "closed")[index % 3],
                "participantType": participant_type,
                "customerId": customer["_id"] if customer else None,
                "visitorSessionId": None if customer else f"visitor-{index + 1:05d}",
                "guestProfile": {
                    "displayName": guest_name,
                    "email": customer["email"] if customer else ctx.factory.email(guest_name, 5000 + index),
                    "phoneNumber": customer["phoneNumber"] if customer else ctx.factory.phone(),
                },
                "assignedStaffUserId": assigned_user["_id"] if assigned_user else None,
                "sourcePage": ("/", "/tours", "/booking/lookup", "/contact")[index % 4],
                "lastMessagePreview": "",
                "lastMessageAt": ctx.factory.iso_timestamp(days_back=2),
                "unreadForAdminCount": 0,
                "unreadForCustomerCount": 0,
                "createdAt": ctx.factory.iso_timestamp(days_back=35),
                "updatedAt": ctx.factory.iso_timestamp(days_back=1),
            }
        )
    ctx.collections["chatConversations"] = items


def build_chat_messages(ctx) -> None:
    conversations = ctx.collections["chatConversations"]
    if not conversations:
        ctx.collections["chatMessages"] = []
        return
    users = {user["_id"]: user for user in ctx.collections["Users"]}
    customers = {customer["_id"]: customer for customer in ctx.collections["Customers"]}
    admin_unread = {conversation["_id"]: 0 for conversation in conversations}
    customer_unread = {conversation["_id"]: 0 for conversation in conversations}
    items = []

    for index in range(ctx.counts["chatMessages"]):
        conversation = conversations[index % len(conversations)]
        customer = customers.get(conversation["customerId"]) if conversation["customerId"] else None
        staff_user = users.get(conversation["assignedStaffUserId"]) if conversation["assignedStaffUserId"] else None
        customer_turn = index % 2 == 0
        if conversation["participantType"] == "guest":
            sender_type = "guest" if customer_turn else "staff"
            sender_user_id = staff_user["_id"] if sender_type == "staff" and staff_user else None
            sender_display = conversation["guestProfile"]["displayName"] if sender_type == "guest" else (staff_user["fullName"] if staff_user else "HV Travel Team")
        else:
            sender_type = "customer" if customer_turn else "staff"
            sender_user_id = customer["_id"] if sender_type == "customer" and customer else (staff_user["_id"] if staff_user else None)
            sender_display = customer["fullName"] if sender_type == "customer" and customer else (staff_user["fullName"] if staff_user else "HV Travel Team")

        is_read = index % 5 != 0
        if not is_read:
            if sender_type == "staff":
                customer_unread[conversation["_id"]] += 1
            else:
                admin_unread[conversation["_id"]] += 1
        sent_at = ctx.factory.iso_timestamp(days_back=10)
        content = ctx.factory.paragraph(1)
        items.append(
            {
                "_id": ctx.factory.object_id(),
                "conversationId": conversation["_id"],
                "senderType": sender_type,
                "senderUserId": sender_user_id,
                "senderDisplayName": sender_display,
                "messageType": "text",
                "content": content,
                "isRead": is_read,
                "readAt": ctx.factory.iso_timestamp(days_back=5) if is_read else None,
                "sentAt": sent_at,
                "createdAt": sent_at,
            }
        )
        conversation["lastMessagePreview"] = content[:120]
        conversation["lastMessageAt"] = sent_at
        conversation["updatedAt"] = sent_at

    for conversation in conversations:
        conversation["unreadForAdminCount"] = admin_unread[conversation["_id"]]
        conversation["unreadForCustomerCount"] = customer_unread[conversation["_id"]]

    ctx.collections["chatMessages"] = items


def build_contact_messages(ctx) -> None:
    items = []
    for index in range(ctx.counts["ContactMessages"]):
        full_name = ctx.factory.person_name()
        sent = index % 4 != 0
        items.append(
            {
                "_id": ctx.factory.object_id(),
                "fullName": full_name,
                "phoneNumber": ctx.factory.phone(),
                "email": ctx.factory.email(full_name, 7000 + index),
                "subject": _CONTACT_SUBJECTS[index % len(_CONTACT_SUBJECTS)],
                "message": ctx.factory.paragraph(2),
                "notificationEmail": "ops@hvtravel.vn",
                "emailSent": sent,
                "emailSentAt": ctx.factory.iso_timestamp(days_back=15) if sent else None,
                "emailError": "" if sent else "SMTP timeout while sending to support mailbox",
                "createdAt": ctx.factory.iso_timestamp(days_back=20),
                "updatedAt": ctx.factory.iso_timestamp(days_back=2),
            }
        )
    ctx.collections["ContactMessages"] = items
