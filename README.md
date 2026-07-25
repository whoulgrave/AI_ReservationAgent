This project came out of a real-world need: I am a member of a small fishing club (50 members) that owns an old mill pond.  We have a small, comfortable cabin on premises that members can reserve for overnight stays for a nominal fee.  Traditionally, reservations were managed with a physical calendar and members would have to visit the cabin to penvil themselves in to the calendar to reserve a night, or telephone the Club Caretaker and ask him to do it for them.  This has never been a great solution as most members live an hour or more from the club property.

This project attempts to solve most, if not all, of the issues with the old system, using inexpensive, commodity, cloud resources and the Hermes AI Agent to provide a modern, intuitive, and robust reservation system providing:

  - reservations by email, text, or telephone, subject to configurable club policy

  - a waitlist with automatic, round-robin, time-limited notification of cabin availability
  
  - management functionality allowing for active member roster management, audit reporting, an usage reporting for billing members for their usage   

While all of this is do-able, it was my desire to provide a solution that was as frugal as possible -given the size of our prganization - and stil provided a robust solution.  Accordingly, a 'monthly budget' of $20 - $30 was set as a constraint for the proposed solution. 

Using ChatGpt, I submitted the following prompt:

"I want to create the following agentic service with Hermes running on a leased VPS: 

1) the purpose of the voice service will be to manage a overnight cabin rental calendar for a small club, having 50 members.
2) there are 50 members, each with a club-assigned identification number.
3) there is one cabin.
4) any member can reserve the cabin for any date as long as it has not been reserved by another member for the same date.
5) no member may have active reservations for more than two nights at one time.
6) members should be able to make a reservation using the following methods: email, text message, or telephone call.
7) the service should confirm the making of a successful reservation by one of the same communication methods. The service should ask the member what their prefered means is and remember it. THe next time the member successfully makes a reservation, the service should confirm their remembered preference and allow them to change it if they like.
8) the service should record all reservations and be able to to report all reservations for a defined period of time when requested by any member authorized to do so.
9) Members should be able to cancel reservations at any time up to noon 24 hours prior to the reservation date.
10) When any member requests a date that has already been reserved, the service will offer the member the opportunity to be placed on a waiting list for that ate. The service should them notify the member should the member holding thr resrvation cancel. In the event that the waiting member fails to accept the date, the next member on the waiting list should be notified and offered a reservation for the date.
11) any member should be able to contact the service and request a list of all calendar dates for a specified period of time and have it sent to them - by their prefered communication method.
12) the service should allow designated members to upload a new roster of active members periodically. From the point of upload, members not on the new roster should be prevented from making new reservations, and any existing resrvations they hold should be cancelled and immediately offered to any active members currently on the waiting list for that date.
13) all actions of the service must be logged and auditable by any member authorized to do so. Please describe the additional services that will be required (for email, phone, text), how to set them up, configure them. Also, make recommendations for additional considerations, or recommended changes and improvements. I would like to be able to get this service up and running with a projected monthly cost under $20 if possible. Please include cost constraints and considerations I may have not considered."

While not part of my orginal plan, I decided to make use of my Azure account for initial development and testing. The current solution reflects that decision.

