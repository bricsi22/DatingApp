import { Component, OnInit } from '@angular/core';
import { Member } from 'src/app/models/members';
import { MemberService } from 'src/app/_services/member.service';

@Component({
  selector: 'app-memberlist',
  templateUrl: './member-list.component.html',
  styleUrls: ['./member-list.component.css']
})
export class MemberListComponent implements OnInit {
   members: Member[];

  constructor(private memberService: MemberService) { }

  ngOnInit(): void {
    this.loadMembers();
  }

  loadMembers() {
    this.memberService.getMembers().subscribe(members => {
      debugger;
      this.members = members;
    })
  }

}
