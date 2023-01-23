﻿using Bit.Api.SecretManagerFeatures.Models.Request;
using Bit.Api.SecretManagerFeatures.Models.Response;
using Bit.Api.Utilities;
using Bit.Core.Context;
using Bit.Core.Enums;
using Bit.Core.Exceptions;
using Bit.Core.Repositories;
using Bit.Core.SecretManagerFeatures.Porting.Interfaces;
using Bit.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bit.Api.Controllers;

[SecretsManager]
public class SecretsManagerPortingController : Controller
{
    private readonly ISecretRepository _secretRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUserService _userService;
    private readonly IImportCommand _importCommand;
    private readonly ICurrentContext _currentContext;

    public SecretsManagerPortingController(ISecretRepository secretRepository, IProjectRepository projectRepository, IUserService userService, IImportCommand importCommand, ICurrentContext currentContext)
    {
        _secretRepository = secretRepository;
        _projectRepository = projectRepository;
        _userService = userService;
        _importCommand = importCommand;
        _currentContext = currentContext;
    }

    [HttpGet("sm/{organizationId}/export")]
    public async Task<SMExportResponseModel> Export([FromRoute] Guid organizationId, [FromRoute] string format = "json")
    {
        var userId = _userService.GetProperUserId(User).Value;
        var orgAdmin = await _currentContext.OrganizationAdmin(organizationId);
        var accessClient = AccessClientHelper.ToAccessClient(_currentContext.ClientType, orgAdmin);

        var projects = await _projectRepository.GetManyByOrganizationIdAsync(organizationId, userId, accessClient);
        var secrets = await _secretRepository.GetManyByOrganizationIdAsync(organizationId); // TODO: add access check for secrets here once it's supported

        if (projects == null && secrets == null)
        {
            throw new NotFoundException();
        }

        return new SMExportResponseModel(projects, secrets);
    }

    [HttpPost("sm/{organizationId}/import")]
    public async Task<SMImportResponseModel> Import([FromRoute] Guid organizationId, [FromBody] SMImportRequestModel importRequest)
    {
        if (!await _currentContext.OrganizationUser(organizationId))
        {
            throw new UnauthorizedAccessException();
        }

        var result = await _importCommand.ImportAsync(organizationId, importRequest.ToSMImport());
        return new SMImportResponseModel(result);
    }
}